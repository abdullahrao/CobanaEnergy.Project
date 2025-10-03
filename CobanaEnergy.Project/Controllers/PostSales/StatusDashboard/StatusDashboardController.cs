using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.PostSales.Entities;
using CobanaEnergy.Project.Models.PostSales.StatusDashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Service.HelperUtilityService;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Odbc;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PostSales.StatusDashboard
{
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

            // Gas contracts
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
            // 1. Combine Electric + Gas Contracts
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
                .GroupBy(s => s.EId)
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            var postSaleObj = await _db.CE_PostSaleObjections
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => s.EId)
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
            // 6. Paging
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
                data = rows
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
                    ObjectionCount = 1,
                    CreatedDate = DateTime.Now,
                    ModifyDate = DateTime.Now
                };
                _db.CE_PostSaleObjections.Add(objection);
                return true;
            }

            objection.ObjectionDate = dto.ObjectionDate;
            objection.QueryType = dto.QueryType;
            objection.ObjectionCount += 1;
            objection.ModifyDate = DateTime.Now;

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
                (x.Department ?? "").ToLower().Contains(searchValue)
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
                var st = statuses.FirstOrDefault(s => s.EId == x.EId);
                var cr = crs.FirstOrDefault(c => c.EId == x.EId);
                var postSales = postSaleObj.FirstOrDefault(p => p.EId == x.EId);
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
                    EmailBody = emailDetails?.EmailBody
                });
            }

            return rows;
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