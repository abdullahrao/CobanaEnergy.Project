﻿using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.AwaitingPaymentsDashboard;
using CobanaEnergy.Project.Models.Accounts.ProblematicsDashboard;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.ProblematicsDashboard
{
    [Authorize(Roles = "Accounts,Controls")]
    public class ProblematicsDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;
        private readonly List<string> problematicsStatuses = new List<string>
        {
            "Commission Hold - Unpaid Bill",
            "Commission Hold - Unpaid Bill Clawback",
            "Discrepancies",
            "In Dispute",
            "Lost Confirmation"
        };

        public ProblematicsDashboardController(ApplicationDBContext _db)
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

            var model = new ProblematicsDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/Accounts/ProblematicsDashboard/ProblematicsDashboard.cshtml", model);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetProblematicsContracts(int? supplierId, DateTime? startDate, DateTime? endDate)
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

                var electricContractsRaw = electricContractsList
                    .Select(e => new
                    {
                        Contract = e,
                        Status = db.CE_ContractStatuses
                            .FirstOrDefault(cs => cs.EId == e.EId && cs.Type == "Electric" && problematicsStatuses.Contains(cs.PaymentStatus)),
                        Reconciliation = db.CE_CommissionAndReconciliation
                            .FirstOrDefault(r => r.EId == e.EId && r.contractType == "Electric")
                    })
                    .Where(x => x.Status != null)
                    .ToList();

                var gasContractsRaw = gasContractsList
                    .Select(g => new
                    {
                        Contract = g,
                        Status = db.CE_ContractStatuses
                            .FirstOrDefault(cs => cs.EId == g.EId && cs.Type == "Gas" && problematicsStatuses.Contains(cs.PaymentStatus)),
                        Reconciliation = db.CE_CommissionAndReconciliation
                            .FirstOrDefault(r => r.EId == g.EId && r.contractType == "Gas")
                    })
                    .Where(x => x.Status != null)
                    .ToList();

                var contracts = electricContractsRaw.Select(x => new ProblematicsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = x.Contract.MPAN,
                    MPRN = null,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = x.Contract.InputDate,
                    StartDate = DateTime.TryParse(x.Reconciliation?.StartDate, out var startDt)
                                ? startDt.ToString("dd/MM/yyyy")
                                : "N/A",
                    CED = x.Reconciliation.CED,
                    CEDCOT = x.Reconciliation.CED_COT,
                    ContractStatus = x.Status.ContractStatus,
                    PaymentStatus = x.Status.PaymentStatus,
                    Invoices = string.Join(", ", db.CE_EacLogs
                            .Where(l => l.EId == x.Contract.EId && l.ContractType == "Electric")
                            .Select(l => l.InvoiceNo)
                            .ToList()),
                    SupplierCobanaInvoiceNotes = x.Reconciliation.SupplierCobanaInvoiceNotes ?? "N/A",
                    CobanaFinalReconciliation = x.Reconciliation.CobanaFinalReconciliation ?? "N/A"
                }).ToList();

                contracts.AddRange(gasContractsRaw.Select(x => new ProblematicsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = null,
                    MPRN = x.Contract.MPRN,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = x.Contract.InputDate,
                    StartDate = DateTime.TryParse(x.Reconciliation?.StartDate, out var startDt)
                                ? startDt.ToString("dd/MM/yyyy")
                                : "N/A",
                    CED = x.Reconciliation.CED,
                    CEDCOT = x.Reconciliation.CED_COT,
                    ContractStatus = x.Status.ContractStatus,
                    PaymentStatus = x.Status.PaymentStatus,
                    Invoices = string.Join(", ", db.CE_EacLogs
                            .Where(l => l.EId == x.Contract.EId && l.ContractType == "Gas")
                            .Select(l => l.InvoiceNo)
                            .ToList()),
                    SupplierCobanaInvoiceNotes = x.Reconciliation.SupplierCobanaInvoiceNotes ?? "N/A",
                    CobanaFinalReconciliation = x.Reconciliation.CobanaFinalReconciliation ?? "N/A"
                }));

                var problematicsCount = contracts.Count;

                return JsonResponse.Ok(new
                {
                    Contracts = contracts,
                    ProblematicsCount = problematicsCount
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetProblematicsContracts: " + ex);
                return JsonResponse.Fail("Error fetching problematics contracts.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateFollowUpDates(List<ContractUpdateModelProblematics> contracts)
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
    }
}