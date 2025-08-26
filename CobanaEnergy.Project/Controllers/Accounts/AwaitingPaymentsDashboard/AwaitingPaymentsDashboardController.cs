﻿using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.AwaitingPaymentsDashboard;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.AwaitingPaymentsDashboard
{
    [Authorize(Roles = "Accounts,Controls")]
    public class AwaitingPaymentsDashboardController : BaseController
    {

        private readonly ApplicationDBContext db;
        public AwaitingPaymentsDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var suppliers = await db.CE_Supplier
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            var model = new AwaitingPaymentsDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/Accounts/AwaitingPaymentsDashboard/AwaitingPaymentsDashboard.cshtml", model);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetAwaitingPaymentsContracts(int? supplierId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var electricContractsQuery = db.CE_ElectricContracts.AsQueryable();
                var gasContractsQuery = db.CE_GasContracts.AsQueryable();

                if (supplierId.HasValue)
                {
                    electricContractsQuery = electricContractsQuery.Where(e => e.SupplierId == supplierId);
                    gasContractsQuery = gasContractsQuery.Where(g => g.SupplierId == supplierId);
                }

                var electricContractsList = await electricContractsQuery.ToListAsync();
                var gasContractsList = await gasContractsQuery.ToListAsync();

                if (startDate.HasValue && endDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    var end = endDate.Value.Date;

                    electricContractsList = electricContractsList
                        .Where(e => DateTime.TryParse(e.InputDate, out var dt) &&
                                    dt.Date >= start && dt.Date <= end)
                        .ToList();

                    gasContractsList = gasContractsList
                        .Where(g => DateTime.TryParse(g.InputDate, out var dt) &&
                                    dt.Date >= start && dt.Date <= end)
                        .ToList();
                }
                else if (startDate.HasValue && !endDate.HasValue)
                {
                    return JsonResponse.Fail("Please select both dates!");
                }

                if (!electricContractsList.Any() && !gasContractsList.Any())
                {
                    return JsonResponse.Ok(new List<AwaitingPaymentsRowViewModel>());
                }
                var electricContractsRaw = electricContractsList
                     .Select(e => new
                     {
                         Contract = e,
                         Status = db.CE_ContractStatuses
                             .FirstOrDefault(cs => cs.EId == e.EId && cs.Type == "Electric"),
                         Reconciliation = db.CE_CommissionAndReconciliation
                             .FirstOrDefault(r => r.EId == e.EId && r.contractType == "Electric")
                     })
                    .Where(x => x.Status != null && ShouldShowOnAwaitingPaymentsDashboard(x.Status.PaymentStatus, x.Status.ModifyDate.ToString(), x.Reconciliation?.CED))
                    .ToList();


                var gasContractsRaw = gasContractsList
                    .Select(g => new
                    {
                        Contract = g,
                        Status = db.CE_ContractStatuses
                            .FirstOrDefault(cs => cs.EId == g.EId && cs.Type == "Gas"),
                        Reconciliation = db.CE_CommissionAndReconciliation
                            .FirstOrDefault(r => r.EId == g.EId && r.contractType == "Gas")
                    })
                    .Where(x => x.Status != null && ShouldShowOnAwaitingPaymentsDashboard(x.Status.PaymentStatus, x.Status.ModifyDate.ToString(), x.Reconciliation?.CED))
                    .ToList();

                var contracts = electricContractsRaw.Select(x => new AwaitingPaymentsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = x.Contract.MPAN,
                    MPRN = null,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = x.Contract.InputDate,
                    ContractType = "Electric",
                    StartDate = DateTime.TryParse(x.Reconciliation?.StartDate, out var startDt)
                                ? startDt.ToString("dd/MM/yyyy")
                                : "N/A",
                    Duration = x.Contract.Duration,
                    PaymentStatus = x.Status.PaymentStatus ?? "N/A",
                    InitialCommissionForecast = x.Reconciliation != null
                         ? db.CE_CommissionMetrics
                             .Where(m => m.ReconciliationId == x.Reconciliation.Id && m.contractType == "Electric")
                             .Select(m => m.InitialCommissionForecast)
                             .FirstOrDefault() ?? "N/A"
                         : "N/A",
                    SupplierCobanaInvoiceNotes = x.Reconciliation?.SupplierCobanaInvoiceNotes ?? "N/A",
                }).ToList();

                contracts.AddRange(gasContractsRaw.Select(x => new AwaitingPaymentsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = null,
                    MPRN = x.Contract.MPRN,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = x.Contract.InputDate,
                    ContractType = "Gas",
                    StartDate = DateTime.TryParse(x.Reconciliation?.StartDate, out var startDt)
                                ? startDt.ToString("dd/MM/yyyy")
                                : "N/A",
                    Duration = x.Contract.Duration,
                    PaymentStatus = x.Status.PaymentStatus ?? "N/A",
                    InitialCommissionForecast = x.Reconciliation != null
                         ? db.CE_CommissionMetrics
                             .Where(m => m.ReconciliationId == x.Reconciliation.Id && m.contractType == "Electric")
                             .Select(m => m.InitialCommissionForecast)
                             .FirstOrDefault() ?? "N/A"
                         : "N/A",
                    SupplierCobanaInvoiceNotes = x.Reconciliation?.SupplierCobanaInvoiceNotes ?? "N/A",
                }));


                #region [Status Count]

                var statuses = HelperUtility.GetStatuses();
                var statusKeys = statuses.Select(s => s.Status).ToList();

                var dbCounts = await db.CE_ContractStatuses
                    .Where(cs => statusKeys.Contains(cs.PaymentStatus))
                    .GroupBy(cs => cs.PaymentStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var result = statuses
                    .Select(s => new
                    {
                        Label = s.Label,
                        Count = dbCounts.FirstOrDefault(dc => dc.Status == s.Status)?.Count ?? 0
                    })
                    .ToList();
                #endregion

                #region [Monthly Counter]


                var monthlyStatus = HelperUtility.GetMonthlyStatus();
                var monthlyStatusKeys = statuses.Select(s => s.Status).ToList();

                var monthlyCounterList = await db.CE_ContractStatuses
                    .Where(cs => statusKeys.Contains(cs.PaymentStatus))
                    .GroupBy(cs => cs.PaymentStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var resultantCounter = monthlyStatus
                    .Select(s => new
                    {
                        Label = s.Label,
                        Count = dbCounts.FirstOrDefault(dc => dc.Status == s.Status)?.Count ?? 0
                    })
                    .ToList();


                #endregion


                return JsonResponse.Ok(new
                {
                    Contracts = contracts,
                    MonthlyCounterList = resultantCounter,
                    CounterList = result
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetAwaitingPaymentsContracts: " + ex);
                return JsonResponse.Fail("Error fetching contracts.");
            }
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateFollowUpDates(List<ContractUpdateModel> contracts)
        {
            try
            {
                if (contracts == null || !contracts.Any())
                    return JsonResponse.Fail("No contracts selected.");

                foreach (var contract in contracts)
                {
                    string contractType = null;

                    if (!string.IsNullOrEmpty(contract.MPAN) && contract.MPAN.Length == 13)
                        contractType = "Electric";
                    else if (!string.IsNullOrEmpty(contract.MPRN) && contract.MPRN.Length >= 6 && contract.MPRN.Length <= 10)
                        contractType = "Gas";

                    if (contractType != null)
                    {
                        var reconciliation = await db.CE_CommissionAndReconciliation
                            .FirstOrDefaultAsync(r => r.EId == contract.EId && r.contractType == contractType);

                        if (reconciliation != null)
                        {
                            DateTime baseDate;

                            if (!string.IsNullOrWhiteSpace(reconciliation.CommissionFollowUpDate) &&
                                DateTime.TryParse(reconciliation.CommissionFollowUpDate, out var existingDate))
                            {
                                baseDate = existingDate;
                            }
                            else
                            {
                                baseDate = DateTime.Now;
                            }
                            var newDate = AddWorkingDays(baseDate, 5);

                            reconciliation.CommissionFollowUpDate = newDate.ToString("yyyy-MM-dd");
                        }
                    }
                }

                await db.SaveChangesAsync();
                return JsonResponse.Ok(message: "Follow-up dates updated.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateFollowUpDates: " + ex);
                return JsonResponse.Fail("Error updating follow-up dates.");
            }
        }


        [HttpGet]
        public async Task<PartialViewResult> EditAwaitingPaymentPopup(string eid, string contractType, string paymentStatus)
        {
            try
            {
                var result = await db.CE_CommissionAndReconciliation.Where(x => x.EId == eid && x.contractType == contractType).FirstOrDefaultAsync();
                if (result == null)
                    return PartialView("~/Views/Shared/_ModalError.cshtml", "Record not found.");
                var model = new EditAwaitingPaymentsViewModel
                {
                    ContractType = result.contractType,
                    EId = result.EId,
                    SupplierCobanaInvoiceNotes = result.SupplierCobanaInvoiceNotes,
                    PaymentStatus = paymentStatus

                };

                return PartialView("~/Views/Accounts/Common/EditCobanaInvoiceNotes.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("EditAwaitingPaymentPopup: " + ex);
                return PartialView("~/Views/Shared/_ModalError.cshtml", "Failed to load popup.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditAwaitingPaymentUpdate(EditAwaitingPaymentsViewModel model)
        {
            try
            {

                var result = await db.CE_CommissionAndReconciliation.Where(x => x.EId == model.EId && x.contractType == model.ContractType).FirstOrDefaultAsync();
                if (result == null)
                    return JsonResponse.Fail(message: " Supplier Cobana Invoice Notes updated.");
                result.SupplierCobanaInvoiceNotes = model.SupplierCobanaInvoiceNotes;

                #region [INSERT PAYMENT LOGS]
                
                var notesModel = new PaymentAndNotesLogsViewModel
                {
                    CobanaInvoiceNotes = model.SupplierCobanaInvoiceNotes,
                    PaymentStatus = model.PaymentStatus,
                    EId = model.EId,
                    ContractType = model.ContractType,
                    Dashboard = "AwaitingDashboardPayment",
                    Username = User?.Identity?.Name ?? "Unknown User"
                };
                PaymentLogsHelper.InsertPaymentAndNotesLogs(db, notesModel);

                
                #endregion

                await db.SaveChangesAsync();
                return JsonResponse.Ok(message: " Supplier Cobana Invoice Notes updated.");
            }
            catch (Exception ex)
            {
                Logger.Log("EditAwaitingPaymentUpdate: " + ex);


                return JsonResponse.Fail("Error updating Supplier Cobana Invoice Notes.");
            }
        }

        private DateTime AddWorkingDays(DateTime start, int workingDays)
        {
            var current = start;
            while (workingDays > 0)
            {
                current = current.AddDays(1);
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays--;
                }
            }
            return current;
        }

        private bool ShouldShowOnAwaitingPaymentsDashboard(string paymentStatus, string startDateStr, string endDateStr)
        {
            
            if (string.IsNullOrWhiteSpace(paymentStatus))
                return false;

            if (!SupportedSuppliers.TryGetWaitDays(paymentStatus, out var waitDays))
                return false;

            DateTime now = DateTime.UtcNow.Date;
            DateTime? startDate = DateTime.TryParse(startDateStr, out var sdt) ? sdt.Date : (DateTime?)null;
            DateTime? endDate = DateTime.TryParse(endDateStr, out var edt) ? edt.Date : (DateTime?)null;

            if (paymentStatus.Equals("Awaiting Final Reconciliation", StringComparison.OrdinalIgnoreCase))
            {
                return endDate.HasValue && now >= endDate.Value.AddDays(waitDays);
            }
            else
            {
                return startDate.HasValue && now >= startDate.Value.AddDays(waitDays);
            }
        }

    }
}