using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.Gas.EditGas;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.PostSales.Entities;
using CobanaEnergy.Project.Models.PostSales.PostSalesContract;
using CobanaEnergy.Project.Models.PostSales.StatusDashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using CobanaEnergy.Project.Service.HelperUtilityService;
using Logic;
using Logic.LockManager;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Odbc;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PostSales.StatusDashboard
{
    [Authorize(Roles = "Post-sales")]
    public class StatusDashboardController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public StatusDashboardController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var suppliers = await _db.CE_Supplier
               .Select(s => new SelectListItem
               {
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync();

            var model = new StatusDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/PostSales/StatusDashboard/StatusDashboard.cshtml", model);
        }


        private async Task EnsurePreSalesMovedToPostSalesAsync()
        {
            // PreSale column me "submitted" or "overturned" check karna hai
            var submittedKeys = new[] { "submitted", "overturned" };

            // Electric contracts
            var electricPresales = await _db.CE_ElectricContracts
                .Where(ec => !string.IsNullOrEmpty(ec.PreSalesStatus) &&
                             submittedKeys.Contains(ec.PreSalesStatus.ToLower()))
                .Select(ec => new { ec.EId, Type = "Electric" })
                .ToListAsync();

            // Contracts
            var gasPresales = await _db.CE_GasContracts
                .Where(gc => !string.IsNullOrEmpty(gc.PreSalesStatus) &&
                             submittedKeys.Contains(gc.PreSalesStatus.ToLower()))
                .Select(gc => new { gc.EId, Type = "Gas" })
                .ToListAsync();

            // Combine both
            var allPresales = electricPresales.Concat(gasPresales).ToList();

            foreach (var s in allPresales)
            {
                var eId = s.EId;
                var type = s.Type;

                // Check if already present in post-sales statuses
                var existingStatus = await _db.CE_ContractStatuses
                    .Where(cs => cs.EId == eId && cs.Type == type)
                    .OrderByDescending(cs => cs.ModifyDate)
                    .FirstOrDefaultAsync();

                if (existingStatus == null)
                {
                    var newStatus = new CE_ContractStatuses
                    {
                        EId = eId,
                        Type = s.Type,
                        ContractStatus = "Pending",
                        PaymentStatus = existingStatus?.PaymentStatus,
                        ModifyDate = DateTime.Now
                    };

                    _db.CE_ContractStatuses.Add(newStatus);
                }
            }

            await _db.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPostSalesContracts(StatusQueryParams q)
        {
            await EnsurePreSalesMovedToPostSalesAsync();

            // -------------------------------------------
            // 1. Combine Electric + Contracts
            // -------------------------------------------
            var combined = GetCombinedContracts();

            // -------------------------------------------
            // 2. Apply Filters
            // -------------------------------------------
            combined = ApplyFilters(combined, q);

            // Materialize after filters
            var list = await combined.ToListAsync();

            // Date filters
            if (q.DateFrom.HasValue)
                list = list.Where(x => DateTime.TryParse(x.InputDate, out var d) && d >= q.DateFrom.Value).ToList();

            if (q.DateTo.HasValue)
                list = list.Where(x => DateTime.TryParse(x.InputDate, out var d) && d <= q.DateTo.Value).ToList();

            // -------------------------------------------
            // 3. Load Related Data
            // -------------------------------------------
            var eIds = list.Select(x => x.EId).Distinct().ToList();

            var crs = await _db.CE_CommissionAndReconciliation.Where(c => eIds.Contains(c.EId)).ToListAsync();
            var statuses = await _db.CE_ContractStatuses
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => new { s.EId, s.Type })
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            var postSaleObj = await _db.CE_PostSaleObjections
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => new { s.EId, s.ContractType })
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            var supplierContacts = await _db.CE_SupplierContacts.ToListAsync();

            // exclude dead/ended
            var activeEids = statuses
                .Where(s => s != null && !new[] { "ended", "dead" }.Contains((s.ContractStatus ?? "").ToLower()))
                .Select(s => s.EId)
                .ToHashSet();

            var contracts = list.Where(c => activeEids.Contains(c.EId)).ToList();

            // ContractStatus filter
            if (!string.IsNullOrEmpty(q.ContractStatus))
            {
                var target = q.ContractStatus.ToLower();
                var matched = statuses
                    .Where(s => s != null && (s.ContractStatus ?? "").ToLower() == target)
                    .Select(s => s.EId).ToHashSet();

                contracts = contracts.Where(c => matched.Contains(c.EId)).ToList();
            }

            // -------------------------------------------
            // 4. Searching
            // -------------------------------------------
            contracts = ApplySearching(contracts, q.Search?.Value);

            // -------------------------------------------
            // 5. Sorting
            // -------------------------------------------
            contracts = ApplySorting(contracts, q, crs, statuses);

            // -------------------------------------------
            // 6. Counter
            // -------------------------------------------

            var statusSummary = GetContractStatusSummary(contracts, statuses);


            // -------------------------------------------
            // 7. Paging
            // -------------------------------------------
            var total = contracts.Count;
            var page = contracts.Skip(q.Start).Take(q.Length).ToList();

            // -------------------------------------------
            // 7. Projection
            // -------------------------------------------
            var rows = ProjectRows(page, statuses, crs, postSaleObj, supplierContacts);

            return Json(new
            {
                draw = q.Draw,
                recordsTotal = total,
                recordsFiltered = total,
                data = rows,
                contractCount = total,
                statusSummary = statusSummary
            });
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdatePostSalesRow(UpdatePostSalesFieldDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EId))
                return Json(new { success = false, message = "Invalid payload" });

            bool updated = false;

            try
            {
                // 1. Update Contract Email
                updated |= await UpdateContractEmail(dto);

                // 2. Reconciliation (insert or update)
                var cr = await UpsertReconciliation(dto);
                updated |= cr != null;

                // 3. Metrics (always ensure entry)
                updated |= await UpsertMetrics(cr);

                // 4. Contract Status
                updated |= await UpdateContractStatus(dto);

                // 5. Objection (insert or update + increment)
                updated |= await UpsertObjection(dto);

                // 6. Always insert StatusDashboardLogs (new log entry)
                updated |= InsertStatusDashboardLog(dto, User.Identity?.Name ?? "System");

                if (updated)
                {
                    await _db.SaveChangesAsync();
                    return JsonResponse.Ok(message: "Contract updated successfully.");
                }

                return JsonResponse.Ok(message: "No update applied");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult GetQueryTypes(string supplier)
        {
            if (string.IsNullOrEmpty(supplier))
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);

            if (HelperService.SupplierQueryTypes.TryGetValue(supplier, out var queries))
                return Json(queries, JsonRequestBehavior.AllowGet);

            return Json(new List<string>(), JsonRequestBehavior.AllowGet);
        }

        #region [Edit Contract]

        [HttpGet]
        public async Task<ActionResult> EditContractPostSales(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();
            try
            {
                var contract = await _db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == id);
                var account = await _db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == id && a.Type == "Gas");

                if (contract == null)
                    return HttpNotFound();

                var snapshot = await _db.CE_GasSupplierSnapshots
                                .Include(s => s.CE_GasSupplierProductSnapshots)
                                .Include(s => s.CE_GasSupplierContactSnapshots)
                                .Include(s => s.CE_GasSupplierUpliftSnapshots)
                                .FirstOrDefaultAsync(s => s.EId == contract.EId);

                if (snapshot == null)
                    return HttpNotFound();

                var savedProductSnapshot = snapshot.CE_GasSupplierProductSnapshots
                                           .FirstOrDefault(p => p.ProductId == contract.ProductId);

                var model = new PostSalesEditViewModel
                {
                    EId = contract.EId,
                    MPRN = contract.MPRN,
                    BusinessName = contract.BusinessName,
                    CustomerName = contract.CustomerName,
                    BusinessDoorNumber = contract.BusinessDoorNumber,
                    BusinessHouseName = contract.BusinessHouseName,
                    BusinessStreet = contract.BusinessStreet,
                    BusinessTown = contract.BusinessTown,
                    BusinessCounty = contract.BusinessCounty,
                    PostCode = contract.PostCode,
                    PhoneNumber1 = contract.PhoneNumber1,
                    PhoneNumber2 = contract.PhoneNumber2,
                    EmailAddress = contract.EmailAddress,
                    InitialStartDate = contract.InitialStartDate,
                    InputDate = contract.InputDate,
                    Uplift = Convert.ToDecimal(contract.Uplift),
                    Duration = contract.Duration,
                    InputEAC = Convert.ToDecimal(contract.InputEAC),
                    UnitRate = Convert.ToDecimal(contract.UnitRate),
                    OtherRate = Convert.ToDecimal(contract.OtherRate),
                    StandingCharge = Convert.ToDecimal(contract.StandingCharge),
                    SortCode = account?.SortCode ?? "",
                    AccountNumber = account?.AccountNumber ?? "",
                    CurrentSupplier = contract.CurrentSupplier,

                    SupplierId = snapshot.SupplierId,
                    ProductId = savedProductSnapshot?.Id ?? 0,
                    SupplierCommsType = savedProductSnapshot?.SupplierCommsType ?? contract.SupplierCommsType,

                    EMProcessor = contract.EMProcessor,
                    ContractChecked = contract.ContractChecked,
                    ContractAudited = contract.ContractAudited,
                    Terminated = contract.Terminated,
                    ContractNotes = contract.ContractNotes,
                    Department = contract.Department,
                    Source = contract.Source,
                    SalesType = contract.SalesType,
                    SalesTypeStatus = contract.SalesTypeStatus,
                    PreSalesStatus = contract.PreSalesStatus,
                    PreSalesFollowUpDate = contract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd"),

                    // Brokerage Details
                    BrokerageId = contract.BrokerageId,
                    OfgemId = contract.OfgemId,

                    // Dynamic Department-based fields
                    CloserId = contract.CloserId,
                    ReferralPartnerId = contract.ReferralPartnerId,
                    SubReferralPartnerId = contract.SubReferralPartnerId,
                    BrokerageStaffId = contract.BrokerageStaffId,
                    IntroducerId = contract.IntroducerId,
                    SubIntroducerId = contract.SubIntroducerId,
                    SubBrokerageId = contract.SubBrokerageId,
                    Collaboration = contract.Collaboration,
                    LeadGeneratorId = contract.LeadGeneratorId,

                    SupplierSnapshot = new SupplierSnapshotViewModel
                    {
                        Id = snapshot.Id,
                        SupplierId = snapshot.SupplierId,
                        EId = snapshot.EId,
                        SupplierName = snapshot.SupplierName,
                        Link = snapshot.SupplierLink,
                        Products = snapshot.CE_GasSupplierProductSnapshots.Select(p => new SupplierProductSnapshotViewModel
                        {
                            Id = p.Id,
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            SupplierCommsType = p.SupplierCommsType,
                            StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                            EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                            Commission = p.Commission
                        }).ToList(),
                        Uplifts = snapshot.CE_GasSupplierUpliftSnapshots.Select(u => new SupplierUpliftSnapshotViewModel
                        {
                            Id = u.Id,
                            Uplift = u.Uplift,
                            FuelType = u.FuelType,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        }).ToList(),
                        Contacts = snapshot.CE_GasSupplierContactSnapshots.Select(c => new SupplierContactSnapshotViewModel
                        {
                            Id = c.Id,
                            ContactName = c.ContactName,
                            Role = c.Role,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Notes = c.Notes
                        }).ToList()
                    }
                };

                return View("~/Views/PostSales/PostSalesContract/EditContractPostSales.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to Load Gas Contract : " + ex.Message);
                return JsonResponse.Fail("Failed to Load Gas Contract");
            }
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<JsonResult> EditContractPostSales(PostSalesEditViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
        //            .SelectMany(kvp => kvp.Value.Errors)
        //            .Select(e => e.ErrorMessage)
        //            .ToList();
        //        return JsonResponse.Fail(string.Join("<br>", errors));
        //    }

        //    using (var transaction = _db.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            var contract = await _db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == model.EId);
        //            if (contract == null) return JsonResponse.Fail("Contract not found.");

        //            var reconciliation = await _db.CE_CommissionAndReconciliation.FirstOrDefaultAsync(r => r.EId == model.EId);
        //            var postSaleObjection = await _db.CE_PostSaleObjections.FirstOrDefaultAsync(o => o.EId == model.EId && o.ContractType == "Gas");
        //            var contractStatus = await _db.CE_ContractStatuses.FirstOrDefaultAsync(cs => cs.EId == model.EId && cs.Type == "Gas");

        //            // Keep previous values for logging comparison
        //            var prevContractStatus = contractStatus?.ContractStatus;
        //            var prevQueryType = postSaleObjection?.QueryType;
        //            var prevCSD = contract.InitialStartDate;
        //            var prevCED = reconciliation?.CED;
        //            var prevCEDCOT = reconciliation?.CED_COT;
        //            var prevObjectionDate = postSaleObjection?.ObjectionDate;
        //            var prevReAppliedDate = postSaleObjection?.ReAppliedDate;

        //            // Update contract editable fields
        //            contract.Source = model.Source;
        //            contract.SalesType = model.SalesType;
        //            contract.SalesTypeStatus = model.SalesTypeStatus;

        //            contract.BusinessName = model.BusinessName;
        //            contract.CustomerName = model.CustomerName;
        //            contract.PostCode = model.PostCode;

        //            contract.PhoneNumber1 = model.PhoneNumber1;
        //            contract.PhoneNumber2 = model.PhoneNumber2;
        //            contract.EmailAddress = model.EmailAddress;

        //            // CSD (StartDate) update on contract (if rules require account checks, implement those checks)
        //            contract.InitialStartDate = model.StartDate;

        //            // Update commission/reconciliation fields
        //            if (reconciliation == null)
        //            {
        //                reconciliation = new CE_CommissionAndReconciliation
        //                {
        //                    EId = model.EId,
        //                    StartDate = model.StartDate,
        //                    CED = model.CED,
        //                    CED_COT = model.CED_COT,
        //                    ModifyDate = DateTime.Now
        //                };
        //                _db.CE_CommissionAndReconciliation.Add(reconciliation);
        //            }
        //            else
        //            {
        //                reconciliation.StartDate = model.StartDate;
        //                reconciliation.CED = model.CED;
        //                reconciliation.CED_COT = model.CED_COT;
        //                reconciliation.ModifyDate = DateTime.Now;
        //            }

        //            // Update rates / numeric fields
        //            contract.Uplift = model.Uplift?.ToString() ?? contract.Uplift;
        //            contract.InputEAC = model.InputEAC?.ToString() ?? contract.InputEAC;

        //            // Update PostSale Objection table
        //            if (postSaleObjection == null)
        //            {
        //                postSaleObjection = new CE_PostSaleObjection
        //                {
        //                    EId = model.EId,
        //                    ContractType = "Gas",
        //                    ObjectionDate = model.ObjectionDate,
        //                    QueryType = model.QueryType,
        //                    ObjectionCount = model.ObjectionCount,
        //                    ReAppliedDate = model.ReAppliedDate,
        //                    ReAppliedCount = model.ReAppliedCount,
        //                    CreatedBy = User.Identity?.Name ?? "Post-Sales",
        //                    CreatedDate = DateTime.Now
        //                };
        //                _db.CE_PostSaleObjections.Add(postSaleObjection);
        //            }
        //            else
        //            {
        //                // Manual overwrite with incoming values allowed
        //                postSaleObjection.ObjectionDate = model.ObjectionDate;
        //                postSaleObjection.QueryType = model.QueryType;
        //                postSaleObjection.ObjectionCount = model.ObjectionCount;
        //                postSaleObjection.ReAppliedDate = model.ReAppliedDate;
        //                postSaleObjection.ReAppliedCount = model.ReAppliedCount;
        //                postSaleObjection.ModifyBy = User.Identity?.Name ?? "Post-Sales";
        //                postSaleObjection.ModifyDate = DateTime.Now;
        //            }

        //            // Handle contract status table
        //            if (contractStatus == null)
        //            {
        //                contractStatus = new CE_ContractStatuses
        //                {
        //                    EId = model.EId,
        //                    Type = "Gas",
        //                    ContractStatus = model.ContractStatus,
        //                    PaymentStatus = null,
        //                    ModifyDate = DateTime.Now
        //                };
        //                _db.CE_ContractStatuses.Add(contractStatus);
        //            }
        //            else
        //            {
        //                // Insert a new row if status changed (we keep history if you prefer)
        //                if (!string.Equals(contractStatus.ContractStatus, model.ContractStatus, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    var newStatusRow = new CE_ContractStatuses
        //                    {
        //                        EId = model.EId,
        //                        Type = "Gas",
        //                        ContractStatus = model.ContractStatus,
        //                        PaymentStatus = contractStatus.PaymentStatus,
        //                        ModifyDate = DateTime.Now
        //                    };
        //                    _db.CE_ContractStatuses.Add(newStatusRow);
        //                }
        //                else
        //                {
        //                    // Update existing record ModifyDate only (or keep as-is)
        //                    contractStatus.ModifyDate = DateTime.Now;
        //                }
        //            }

        //            // Special ReApplied logic: if status chosen is "Reapplied - Awaiting Confirmation", set ReAppliedDate to today
        //            if (!string.IsNullOrWhiteSpace(model.ContractStatus) &&
        //                model.ContractStatus.Equals("Reapplied - Awaiting Confirmation", StringComparison.OrdinalIgnoreCase))
        //            {
        //                var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
        //                postSaleObjection.ReAppliedDate = todayStr;
        //            }

        //            // Create post-sales log if relevant fields changed
        //            var changedFields = new System.Text.StringBuilder();
        //            if (!string.Equals(prevContractStatus ?? "", model.ContractStatus ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("ContractStatus, ");
        //            if (!string.Equals(prevQueryType ?? "", model.QueryType ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("QueryType, ");
        //            if (!string.Equals(prevCSD ?? "", model.StartDate ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("StartDate(CSD), ");
        //            if (!string.Equals(prevCED ?? "", reconciliation?.CED ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("CED, ");
        //            if (!string.Equals(prevCEDCOT ?? "", reconciliation?.CED_COT ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("CED(COT), ");
        //            if (!string.Equals(prevObjectionDate ?? "", postSaleObjection?.ObjectionDate ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("ObjectionDate, ");
        //            if (!string.Equals(prevReAppliedDate ?? "", postSaleObjection?.ReAppliedDate ?? "", StringComparison.OrdinalIgnoreCase))
        //                changedFields.Append("ReAppliedDate, ");

        //            if (changedFields.Length > 0)
        //            {
        //                var summary = changedFields.ToString().Trim().TrimEnd(',');
        //                // Add log entry
        //                _db.CE_PostSalesLogs.Add(new CE_PostSalesLogs
        //                {
        //                    ContractType = "Gas",
        //                    EId = model.EId,
        //                    ContractStatus = model.ContractStatus,
        //                    CSD = model.StartDate,
        //                    CED = reconciliation?.CED,
        //                    COT = reconciliation?.CED_COT,
        //                    ReAppliedDate = postSaleObjection?.ReAppliedDate,
        //                    ObjectionDate = postSaleObjection?.ObjectionDate,
        //                    CreatedBy = User.Identity?.Name ?? "Post-Sales",
        //                    CreatedDate = DateTime.Now
        //                });
        //            }

        //            // Update contract notes and EMProcessor/followup fields
        //            contract.ContractNotes = model.ContractNotes;
        //            contract.EMProcessor = model.EMProcessor ?? contract.EMProcessor;
        //            // FollowUpDate (store in reconciliation.CommissionFollowUpDate as per your earlier mapping)
        //            if (reconciliation != null)
        //            {
        //                reconciliation.CommissionFollowUpDate = model.FollowUpDate;
        //            }

        //            // updated timestamp
        //            contract.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        //            await _db.SaveChangesAsync();
        //            transaction.Commit();

        //            return JsonResponse.Ok(new
        //            {
        //                Message = "Post-sales gas contract updated successfully."
        //            }, "Gas Post-Sales updated successfully.");
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();
        //            Logger.Log("EditGasPostSales failed: " + ex.Message);
        //            return JsonResponse.Fail("Error occurred while updating the gas post-sales contract.");
        //        }
        //    }
        //}

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls,Post-Sales,Accounts")]
        public async Task<JsonResult> GetLogsForPostSales(string eid)
        {
            try
            {
                var logs = await _db.CE_PostSalesLogs
                    .Where(l => l.EId == eid && l.ContractType == "Gas")
                    .Select(l => new
                    {
                        l.CreatedBy,
                        l.CreatedDate,
                        l.ContractStatus,
                        l.CSD,
                        l.CED,
                        l.COT,
                        l.ObjectionDate,
                        l.ReAppliedDate
                    })
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();

                var formatted = logs.Select(l => new
                {
                    Username = l.CreatedBy,
                    ActionDate = l.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss"),
                    ContractStatus = l.ContractStatus,
                    CSD = l.CSD,
                    CED = l.CED,
                    COT = l.COT,
                    ObjectionDate = l.ObjectionDate,
                    ReAppliedDate = l.ReAppliedDate
                });

                return JsonResponse.Ok(formatted);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForPostSales failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve post-sales logs.");
            }
        }

        #endregion

        #region [Helper Method]

        private async Task<bool> UpdateContractEmail(UpdatePostSalesFieldDto dto)
        {
            if (dto.ContractType == "Electric")
            {
                var contract = await _db.CE_ElectricContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId && x.Id == dto.ContractId);

                if (contract != null)
                {
                    contract.EmailAddress = dto.Email;
                    return true;
                }
            }
            else
            {
                var contract = await _db.CE_GasContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId && x.Id == dto.ContractId);

                if (contract != null)
                {
                    contract.EmailAddress = dto.Email;
                    return true;
                }
            }

            return false;
        }

        private async Task<CE_CommissionAndReconciliation> UpsertReconciliation(UpdatePostSalesFieldDto dto)
        {
            var cr = await _db.CE_CommissionAndReconciliation
                .Include(x => x.Metrics)
                .FirstOrDefaultAsync(c => c.EId == dto.EId && c.contractType == dto.ContractType);

            if (cr == null)
            {
                cr = new CE_CommissionAndReconciliation
                {
                    EId = dto.EId,
                    contractType = dto.ContractType
                };
                _db.CE_CommissionAndReconciliation.Add(cr);
            }

            if (DateTime.TryParse(dto.StartDate, out var sd))
                cr.StartDate = sd.ToString("yyyy-MM-dd");

            if (DateTime.TryParse(dto.CED, out var ced))
                cr.CED = ced.ToString("yyyy-MM-dd");
            else if (!string.IsNullOrWhiteSpace(dto.StartDate) && int.TryParse(dto.Duration, out var durYears))
                cr.CED = sd.AddYears(durYears).AddDays(-1).ToString("yyyy-MM-dd");

            if (DateTime.TryParse(dto.COTDate, out var cot))
                cr.CED_COT = cot.ToString("yyyy-MM-dd");

            return cr;
        }

        private async Task<bool> UpsertMetrics(CE_CommissionAndReconciliation cr)
        {
            if (cr == null) return false;

            var metrics = await _db.CE_CommissionMetrics
                .FirstOrDefaultAsync(m => m.ReconciliationId == cr.Id && m.contractType == cr.contractType);

            if (metrics == null)
            {
                metrics = new CE_CommissionMetrics
                {
                    ReconciliationId = cr.Id,
                    contractType = cr.contractType
                };
                _db.CE_CommissionMetrics.Add(metrics);
                return true;
            }

            return false;
        }

        private async Task<bool> UpdateContractStatus(UpdatePostSalesFieldDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ContractStatus)) return false;

            var status = await _db.CE_ContractStatuses
                .FirstOrDefaultAsync(s => s.EId == dto.EId && s.Type == dto.ContractType);

            if (status != null)
            {
                status.ContractStatus = dto.ContractStatus;
                status.ModifyDate = DateTime.Now;
                return true;
            }

            return false;
        }

        private async Task<bool> UpsertObjection(UpdatePostSalesFieldDto dto)
        {
            if (dto.ObjectionDate == null && string.IsNullOrWhiteSpace(dto.QueryType))
                return false;

            var objection = await _db.CE_PostSaleObjections
                .FirstOrDefaultAsync(o => o.EId == dto.EId && o.ContractType == dto.ContractType);

            if (objection == null)
            {
                objection = new CE_PostSaleObjection
                {
                    EId = dto.EId,
                    ContractType = dto.ContractType,
                    QueryType = dto.QueryType,
                    ObjectionDate = dto.ObjectionDate,
                    ObjectionCount = dto.ObjectionDate == null ? 0 : 1,
                    ReAppliedDate = dto.ReAppliedDate,
                    ReAppliedCount = dto.ReAppliedDate == null ? 0 : 1,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.Now,
                    ModifyDate = DateTime.Now
                };

                _db.CE_PostSaleObjections.Add(objection);
                return true;
            }

            objection.ObjectionDate = dto.ObjectionDate;
            objection.QueryType = dto.QueryType;
            if (dto.ObjectionDate != null)
                objection.ObjectionCount += 1;
            if (dto.ReAppliedDate != null)
                objection.ReAppliedCount += 1;
            objection.ModifyDate = DateTime.Now;
            objection.ModifyBy = User.Identity?.Name ?? "System";

            return true;
        }

        private bool InsertStatusDashboardLog(UpdatePostSalesFieldDto dto, string userName)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.EId))
                return false;

            var log = new CE_PostSalesLogs
            {
                EId = dto.EId,
                ContractType = dto.ContractType,
                ContractStatus = dto.ContractStatus,
                CSD = dto.StartDate,
                CED = dto.CED,
                COT = dto.COTDate,
                ReAppliedDate = dto.ReAppliedDate,
                ObjectionDate = dto.ObjectionDate,
                CreatedBy = userName,
                CreatedDate = DateTime.Now
            };

            _db.CE_PostSalesLogs.Add(log);
            return true;
        }

        // GET POST SALES ROW

        private IQueryable<ContractViewModel> GetCombinedContracts()
        {
            var electric = from ec in _db.CE_ElectricContracts
                           join sp in _db.CE_Supplier on ec.SupplierId equals sp.Id
                           select new ContractViewModel
                           {
                               Id = ec.Id,
                               EId = ec.EId,
                               ContractType = "Electric",
                               BusinessName = ec.BusinessName,
                               SupplierId = ec.SupplierId,
                               SupplierName = sp.Name,
                               Department = ec.Department,
                               CloserId = ec.CloserId,
                               BrokerageId = ec.BrokerageId,
                               BrokerageStaffId = ec.BrokerageStaffId,
                               IntroducerId = ec.IntroducerId,
                               SubBrokerageId = ec.SubBrokerageId,
                               SubIntroducerId = ec.SubIntroducerId,
                               SubReferralPartnerId = ec.SubReferralPartnerId,
                               ReferralPartnerId = ec.ReferralPartnerId,
                               LeadGeneratorId = ec.LeadGeneratorId,
                               EmailAddress = ec.EmailAddress,
                               PostCode = ec.PostCode,
                               Duration = ec.Duration,
                               MPXN = ec.MPAN,
                               InputDate = ec.InputDate,
                               StartDate = ec.InitialStartDate,
                               ContractNotes = ec.ContractNotes,
                               Link = sp.Link
                           };

            var gas = from gc in _db.CE_GasContracts
                      join sp in _db.CE_Supplier on gc.SupplierId equals sp.Id
                      select new ContractViewModel
                      {
                          Id = gc.Id,
                          EId = gc.EId,
                          ContractType = "Gas",
                          BusinessName = gc.BusinessName,
                          SupplierId = gc.SupplierId,
                          SupplierName = sp.Name,
                          Department = gc.Department,
                          CloserId = gc.CloserId,
                          BrokerageId = gc.BrokerageId,
                          BrokerageStaffId = gc.BrokerageStaffId,
                          IntroducerId = gc.IntroducerId,
                          SubBrokerageId = gc.SubBrokerageId,
                          SubIntroducerId = gc.SubIntroducerId,
                          SubReferralPartnerId = gc.SubReferralPartnerId,
                          ReferralPartnerId = gc.ReferralPartnerId,
                          LeadGeneratorId = gc.LeadGeneratorId,
                          EmailAddress = gc.EmailAddress,
                          PostCode = gc.PostCode,
                          Duration = gc.Duration,
                          MPXN = gc.MPRN,
                          InputDate = gc.InputDate,
                          StartDate = gc.InitialStartDate,
                          ContractNotes = gc.ContractNotes,
                          Link = sp.Link
                      };

            return electric.Concat(gas);
        }

        private IQueryable<ContractViewModel> ApplyFilters(IQueryable<ContractViewModel> combined, StatusQueryParams q)
        {
            if (!string.IsNullOrEmpty(q.Department))
                combined = combined.Where(x => x.Department == q.Department);

            if (!string.IsNullOrEmpty(q.Supplier))
            {
                if (int.TryParse(q.Supplier, out var sId))
                    combined = combined.Where(x => x.SupplierId == sId);
                else
                    combined = combined.Where(x => x.SupplierId.ToString() == q.Supplier);
            }

            if (!string.IsNullOrEmpty(q.BrokerageId) && int.TryParse(q.BrokerageId, out var bId))
                combined = combined.Where(x => x.BrokerageId == bId);

            if (!string.IsNullOrEmpty(q.StaffId) && int.TryParse(q.StaffId, out var stId))
                combined = combined.Where(x => x.BrokerageStaffId == stId);

            if (!string.IsNullOrEmpty(q.SubBrokerageId) && int.TryParse(q.SubBrokerageId, out var sbId))
                combined = combined.Where(x => x.SubBrokerageId == sbId);

            if (!string.IsNullOrEmpty(q.CloserId) && int.TryParse(q.CloserId, out var clId))
                combined = combined.Where(x => x.CloserId == clId);

            if (!string.IsNullOrEmpty(q.LeadGeneratorId) && int.TryParse(q.LeadGeneratorId, out var lgId))
                combined = combined.Where(x => x.LeadGeneratorId == lgId);

            if (!string.IsNullOrEmpty(q.ReferralPartnerId) && int.TryParse(q.ReferralPartnerId, out var rpId))
                combined = combined.Where(x => x.ReferralPartnerId == rpId);

            if (!string.IsNullOrEmpty(q.SubReferralPartnerId) && int.TryParse(q.SubReferralPartnerId, out var subRefId))
                combined = combined.Where(x => x.SubReferralPartnerId == subRefId);

            if (!string.IsNullOrEmpty(q.IntroducerId) && int.TryParse(q.IntroducerId, out var introId))
                combined = combined.Where(x => x.IntroducerId == introId);

            if (!string.IsNullOrEmpty(q.SubIntroducerId) && int.TryParse(q.SubIntroducerId, out var subIntroId))
                combined = combined.Where(x => x.SubIntroducerId == subIntroId);

            return combined;
        }

        private List<ContractViewModel> ApplySearching(List<ContractViewModel> contracts, string searchValue)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return contracts;

            searchValue = searchValue.ToLower();

            return contracts.Where(x =>
                (x.BusinessName ?? "").ToLower().Contains(searchValue) ||
                (x.SupplierName ?? "").ToLower().Contains(searchValue) ||
                (x.MPXN ?? "").ToLower().Contains(searchValue) ||
                (x.EmailAddress ?? "").ToLower().Contains(searchValue) ||
                (x.PostCode ?? "").ToLower().Contains(searchValue) ||
                (x.Department ?? "").ToLower().Contains(searchValue) ||
                (x.ContractNotes ?? "").ToLower().Contains(searchValue)
            ).ToList();
        }

        private List<ContractViewModel> ApplySorting(List<ContractViewModel> contracts, StatusQueryParams q,
            List<CE_CommissionAndReconciliation> crs, List<CE_ContractStatuses> statuses)
        {
            var sortColumn = q.Columns?[q.Order?[0].Column ?? 0].Data;
            var sortDir = q.Order?[0].Dir?.ToLower();

            Func<ContractViewModel, object> keySelector = x => null;

            switch (sortColumn)
            {
                case "SupplierName": keySelector = x => x.SupplierName; break;
                case "BusinessName": keySelector = x => x.BusinessName; break;
                case "MPXN": keySelector = x => x.MPXN; break;
                case "InputDate": keySelector = x => x.InputDate; break;
                case "StartDate": keySelector = x => x.StartDate; break;
                case "CED": keySelector = x => crs.FirstOrDefault(c => c.EId == x.EId)?.CED; break;
                case "COTDate": keySelector = x => crs.FirstOrDefault(c => c.EId == x.EId)?.CED_COT; break;
                case "ContractStatus": keySelector = x => statuses.FirstOrDefault(s => s.EId == x.EId)?.ContractStatus ?? ""; break;
                case "ContractNotes": keySelector = x => x.ContractNotes; break;
                default: keySelector = x => x.BusinessName; break; // fallback
            }

            return (sortDir == "desc")
                ? contracts.OrderByDescending(keySelector).ToList()
                : contracts.OrderBy(keySelector).ToList();
        }

        private List<PostSalesRowViewModel> ProjectRows(
            List<ContractViewModel> page,
            List<CE_ContractStatuses> statuses,
            List<CE_CommissionAndReconciliation> crs,
            List<CE_PostSaleObjection> postSaleObj,
            List<CE_SupplierContacts> supplierContacts)
        {
            var rows = new List<PostSalesRowViewModel>();

            foreach (var x in page)
            {
                var st = statuses.FirstOrDefault(s => s.EId == x.EId && s.Type == x.ContractType);
                var cr = crs.FirstOrDefault(c => c.EId == x.EId);
                var postSales = postSaleObj.FirstOrDefault(p => p.EId == x.EId && p.ContractType == x.ContractType);
                var emailList = supplierContacts
                    .Where(sc => sc.SupplierId == x.SupplierId)
                    .Select(sc => sc.Email)
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct()
                    .ToList();

                // Resolve Agent Name + Email
                var (agentName, agentEmail) = ResolveAgent(x);

                // Resolve Collaboration
                var collaborationName = ResolveCollaboration(x);

                // Supplier Email Template
                string queryType = postSales?.QueryType;
                var emailDetails = _db.CE_EmailTemplateLookups
                    .FirstOrDefault(e => e.SupplierId == x.SupplierId && e.QueryType == queryType);


                string ced = null;
                if (!string.IsNullOrWhiteSpace(cr?.CED))
                    ced = cr.CED;
                else if (DateTime.TryParse(x.StartDate, out var sdt) && int.TryParse(x.Duration?.ToString(), out var durYears))
                    ced = sdt.AddYears(durYears).AddDays(-1).ToString("yyyy-MM-dd");

                rows.Add(new PostSalesRowViewModel
                {
                    ContractId = x.Id,
                    EId = x.EId,
                    SupplierId = x.SupplierId,
                    Agent = agentName, // simplified for demo
                    AgentEmail = agentEmail,
                    Collaboration = collaborationName,
                    CobanaSalesType = x.Department,
                    MPXN = x.MPXN,
                    Duration = x.Duration,
                    SupplierName = x.SupplierName,
                    BusinessName = x.BusinessName,
                    InputDate = x.InputDate,
                    StartDate = cr?.StartDate ?? x.StartDate,
                    PostCode = x.PostCode ?? "-",
                    CED = ced,
                    COTDate = cr?.CED_COT,
                    Email = x.EmailAddress ?? "-",
                    ContractStatus = st?.ContractStatus ?? "-",
                    ObjectionCount = postSales?.ObjectionCount ?? 0,
                    ObjectionDate = postSales?.ObjectionDate,
                    QueryType = postSales?.QueryType ?? "-",
                    ContractType = x.ContractType,
                    EmailList = emailList,
                    EmailSubject = emailDetails?.Subject,
                    EmailBody = emailDetails?.EmailBody,
                    ContractNotes = x.ContractNotes,
                    Link = x.Link
                });
            }

            return rows;
        }

        private List<ContractStatusSummary> GetContractStatusSummary(
                     List<ContractViewModel> contracts,
                     List<CE_ContractStatuses> statuses)
        {
            var counterList = ContractStatusHelper.AllStatuses
                .ToDictionary(kv => kv.Key, kv => 0);

            foreach (var contract in contracts)
            {
                var matched = statuses.FirstOrDefault(s => s.EId == contract.EId && s.Type == contract.ContractType);
                var currentStatus = matched?.ContractStatus ?? "Unknown";

                if (counterList.ContainsKey(currentStatus))
                    counterList[currentStatus]++;
                else
                    counterList[currentStatus] = 1; // handle new/unknown statuses
            }

            // Step 3: Build summary list with color codes
            var summaryList = counterList
                .Select(x => new ContractStatusSummary
                {
                    ContractStatus = x.Key,
                    Count = x.Value,
                    ColorCode = ContractStatusHelper.AllStatuses.TryGetValue(x.Key, out var color)
                        ? color
                        : "#CCCCCC" // fallback for unknowns
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            return summaryList;
        }


        private (string agentName, string agentEmail) ResolveAgent(ContractViewModel x)
        {
            string agentName = "-";
            string agentEmail = "-";

            if (x.Department == "In House" && x.CloserId.HasValue)
            {
                var closer = _db.CE_Sector
                    .Where(s => s.SectorID == x.CloserId && s.SectorType == "closer")
                    .Select(s => new { s.Name, s.Email })
                    .FirstOrDefault();

                agentName = closer?.Name ?? "-";
                agentEmail = closer?.Email ?? "-";
            }
            else if (x.Department == "Brokers" && x.BrokerageStaffId.HasValue)
            {
                var staff = _db.CE_BrokerageStaff
                    .Where(bs => bs.BrokerageStaffID == x.BrokerageStaffId)
                    .Select(bs => new { bs.BrokerageStaffName, bs.Email })
                    .FirstOrDefault();

                agentName = staff?.BrokerageStaffName ?? "-";
                agentEmail = staff?.Email ?? "-";
            }
            else if (x.Department == "Introducers" && x.SubIntroducerId.HasValue)
            {
                var intro = _db.CE_SubIntroducer
                    .Where(si => si.SubIntroducerID == x.SubIntroducerId)
                    .Select(si => new { si.SubIntroducerName, si.SubIntroducerEmail })
                    .FirstOrDefault();

                agentName = intro?.SubIntroducerName ?? "-";
                agentEmail = intro?.SubIntroducerEmail ?? "-";
            }

            return (agentName, agentEmail);
        }

        private string ResolveCollaboration(ContractViewModel x)
        {
            string collaborationName = "-";

            if (x.Department == "In House")
            {
                if (x.LeadGeneratorId.HasValue)
                {
                    collaborationName = _db.CE_Sector
                        .Where(lg => lg.SectorID == x.LeadGeneratorId && lg.SectorType == "Leads Generator")
                        .Select(lg => lg.Name)
                        .FirstOrDefault() ?? "-";
                }
                if (x.ReferralPartnerId.HasValue)
                {
                    collaborationName = _db.CE_Sector
                        .Where(rp => rp.SectorID == x.ReferralPartnerId && rp.SectorType == "Referral Partner")
                        .Select(rp => rp.Name)
                        .FirstOrDefault() ?? "-";
                }
            }
            else if (x.Department == "Brokers" && x.SubBrokerageId.HasValue)
            {
                collaborationName = _db.CE_SubBrokerage
                    .Where(sb => sb.SubBrokerageID == x.SubBrokerageId)
                    .Select(sb => sb.SubBrokerageName)
                    .FirstOrDefault() ?? "-";
            }
            else if (x.Department == "Introducers" && x.SubIntroducerId.HasValue)
            {
                collaborationName = _db.CE_SubIntroducer
                    .Where(si => si.SubIntroducerID == x.SubIntroducerId)
                    .Select(si => si.SubIntroducerName)
                    .FirstOrDefault() ?? "-";
            }

            return collaborationName;
        }

        #endregion
    }
}