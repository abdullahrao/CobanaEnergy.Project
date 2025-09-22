using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using CobanaEnergy.Project.Models.Accounts.ProblematicsDashboard;
using CobanaEnergy.Project.Service.ExtensionService;
using Logic.ResponseModel.Helper;
using NPOI.POIFS.Crypt.Dsig;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CobanaEnergy.Project.Controllers.Accounts.MasterDashboard
{
    [Authorize(Roles = "Accounts")]
    public class AccountMasterDashboardController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public AccountMasterDashboardController(ApplicationDBContext db)
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

            var model = new AccountMasterDashboardViewModel
            {
                Suppliers = suppliers
            };


            return View("~/Views/Accounts/MasterDashboard/AccountMasterDashboard.cshtml", model);
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetContracts(ContractQueryParams q)
        {
            // ELECTRIC projection
            var electric =
                from ec in _db.CE_ElectricContracts
                join sp in _db.CE_Supplier
                  on ec.SupplierId equals sp.Id
                select new
                {
                    Id = ec.Id,
                    EId = ec.EId,
                    MPXN = ec.MPAN,
                    ContractType = "Electric",
                    BusinessName = ec.BusinessName,
                    SupplierId = ec.SupplierId,
                    SupplierName = sp.Name,
                    Department = ec.Department,
                    InputDate = ec.InputDate,
                    StartDate = ec.InitialStartDate,
                    InputEAC = ec.InputEAC,
                    BrokerageId = ec.BrokerageId,
                    BrokerageStaffId = ec.BrokerageStaffId,
                    SubBrokerageId = ec.SubBrokerageId,
                    CloserId = ec.CloserId,
                    LeadGeneratorId = ec.LeadGeneratorId,
                    ReferralPartnerId = ec.ReferralPartnerId,
                    SubReferralPartnerId = ec.SubReferralPartnerId,
                    IntroducerId = ec.IntroducerId,
                    SubIntroducerId = ec.SubIntroducerId
                };

            // GAS projection
            var gas =
                from gc in _db.CE_GasContracts
                join sp in _db.CE_Supplier
                    on gc.SupplierId equals sp.Id
                select new
                {
                    Id = gc.Id,
                    EId = gc.EId,
                    MPXN = gc.MPRN,
                    ContractType = "Gas",
                    BusinessName = gc.BusinessName,
                    SupplierId = gc.SupplierId,
                    SupplierName = sp.Name,
                    Department = gc.Department,
                    InputDate = gc.InputDate,
                    StartDate = gc.InitialStartDate,
                    InputEAC = gc.InputEAC,
                    BrokerageId = gc.BrokerageId,
                    BrokerageStaffId = gc.BrokerageStaffId,
                    SubBrokerageId = gc.SubBrokerageId,
                    CloserId = gc.CloserId,
                    LeadGeneratorId = gc.LeadGeneratorId,
                    ReferralPartnerId = gc.ReferralPartnerId,
                    SubReferralPartnerId = gc.SubReferralPartnerId,
                    IntroducerId = gc.IntroducerId,
                    SubIntroducerId = gc.SubIntroducerId
                };

            // COMBINE ELECTRIC + GAS
            var combinedContracts = electric.Concat(gas);

            // JOIN with reconciliation + metrics
            var combined =
                from c in combinedContracts
                join cr in _db.CE_CommissionAndReconciliation
                    on c.EId equals cr.EId into crGroup
                from cr in crGroup.DefaultIfEmpty()
                join m in _db.CE_CommissionMetrics
                    on cr.Id equals m.ReconciliationId into mGroup
                from m in mGroup.DefaultIfEmpty()
                select new ContractViewModel
                {
                    Id = c.Id.ToString(),
                    EId = c.EId,
                    ContractType = c.ContractType,
                    BusinessName = c.BusinessName,
                    SupplierId = c.SupplierId,
                    MPXN = c.MPXN,
                    SupplierName = c.SupplierName,
                    Department = c.Department,
                    InputDate = c.InputDate,
                    StartDate = c.StartDate,
                    InputEAC = c.InputEAC,
                    BrokerageId = c.BrokerageId,
                    BrokerageStaffId = c.BrokerageStaffId,
                    SubBrokerageId = c.SubBrokerageId,
                    CloserId = c.CloserId,
                    LeadGeneratorId = c.LeadGeneratorId,
                    ReferralPartnerId = c.ReferralPartnerId,
                    SubReferralPartnerId = c.SubReferralPartnerId,
                    IntroducerId = c.IntroducerId,
                    SubIntroducerId = c.SubIntroducerId,

                    // Reconciliation
                    ReconciliationId = cr.Id,
                    CED = cr.CED,
                    COTDate = cr.CED_COT,
                    CobanaDueCommission = cr.CobanaDueCommission,
                    CobanaFinalReconciliation = cr.CobanaFinalReconciliation,

                    // Metrics
                    ContractDurationDays = m.ContractDurationDays,
                    LiveDays = m.LiveDays,
                    PercentLiveDays = m.PercentLiveDays,
                    TotalCommissionForecast = m.TotalCommissionForecast,
                    InitialCommissionForecast = m.InitialCommissionForecast,
                    TotalAverageEAC = m.TotalAverageEAC
                };

            // ---- FILTERS ----
            if (!string.IsNullOrEmpty(q.Supplier))
            {
                if (int.TryParse(q.Supplier, out var sId))
                    combined = combined.Where(x => x.SupplierId == sId);
                else
                    combined = combined.Where(x => x.SupplierId.ToString() == q.Supplier);
            }

            if (!string.IsNullOrEmpty(q.Department))
                combined = combined.Where(x => x.Department == q.Department);

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

            if (!string.IsNullOrEmpty(q.IntroducerId) && int.TryParse(q.IntroducerId, out var introId))
                combined = combined.Where(x => x.IntroducerId == introId);

            if (!string.IsNullOrEmpty(q.SubIntroducerId) && int.TryParse(q.SubIntroducerId, out var subIntroId))
                combined = combined.Where(x => x.SubIntroducerId == subIntroId);

            var orderedContracts = combined.ApplyOrdering(q);
            if (q.Order == null || !q.Order.Any())
                orderedContracts = orderedContracts.OrderByDescending(x => x.InputDate);
            var allContracts = await orderedContracts.ToListAsync();

            // Apply Date filters
            if (q.DateFrom.HasValue)
                allContracts = allContracts
                    .Where(x => DateTime.TryParse(x.InputDate, out var d) && d >= q.DateFrom.Value)
                    .ToList();

            if (q.DateTo.HasValue)
                allContracts = allContracts
                    .Where(x => DateTime.TryParse(x.InputDate, out var d) && d <= q.DateTo.Value)
                    .ToList();

            // Get all statuses for these contracts
            var allIds = allContracts.Select(c => c.EId).Distinct().ToList();
            var allStatuses = new List<CE_ContractStatuses>(); 
            if (allIds.Any())
            {
                allStatuses = await _db.CE_ContractStatuses
                    .Where(s => allIds.Contains(s.EId))
                    .GroupBy(s => s.EId)
                    .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                    .ToListAsync();
            }

            // ContractStatus filter
            if (!string.IsNullOrEmpty(q.ContractStatus))
            {
                var statusMap = allStatuses
                    .Where(s => s != null && s.ContractStatus == q.ContractStatus)
                    .Select(s => s.EId)
                    .ToHashSet();

                allContracts = allContracts
                    .Where(x => statusMap.Contains(x.EId))
                    .ToList();
            }

            // Paging
            var total = allContracts.Count();
            var page = allContracts.Skip(q.Start).Take(q.Length).ToList();
            var ids = page.Select(x => x.EId).ToList();

            var statuses = allStatuses.Where(s => ids.Contains(s.EId)).ToList();

            var eacGroups = await _db.CE_EacLogs
                .Where(l => ids.Contains(l.EId))
                .GroupBy(l => l.EId)
                .Select(g => new
                {
                    EId = g.Key,
                    Latest = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault(),
                    All = g.ToList()
                }).ToListAsync();

            var crs = await _db.CE_CommissionAndReconciliation
                .Where(c => ids.Contains(c.EId))
                .ToListAsync();

            var reconciliationIds = crs.Select(c => c.Id).Distinct().ToList();

            var metrics = await _db.CE_CommissionMetrics
                .Where(m => reconciliationIds.Contains(m.ReconciliationId))
                .ToListAsync();

            var data = page.Select(x =>
            {
                var eacGroup = eacGroups.FirstOrDefault(e => e.EId == x.EId);
                var latestEac = eacGroup?.Latest;
                var status = statuses.FirstOrDefault(s => s.EId == x.EId);
                var cr = crs.FirstOrDefault(c => c.EId == x.EId);
                var metric = metrics.FirstOrDefault(m => m.ReconciliationId == cr?.Id);

                string prevInvNumbers = "-";
                decimal cobanaPaidSum = 0m;
                if (eacGroup != null && eacGroup.All.Any())
                {
                    var invs = eacGroup.All
                        .Where(i => !string.IsNullOrEmpty(i.InvoiceNo))
                        .Select(i => i.InvoiceNo)
                        .Distinct();
                    prevInvNumbers = invs.Any() ? string.Join(", ", invs) : "-";

                    cobanaPaidSum = eacGroup.All
                        .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceAmount))
                        .Select(i => decimal.TryParse(i.InvoiceAmount, out var val) ? val : 0)
                        .Sum();
                }

                string supplierEACDisplay = "-";
                if (latestEac != null)
                {
                    var prefix = !string.IsNullOrEmpty(latestEac.SupplierEac) ? latestEac.SupplierEac + ":" : "";
                    supplierEACDisplay = latestEac.EacValue != null
                        ? $"{prefix}{latestEac.SupplierEac ?? ""}-{latestEac.EacValue}"
                        : (latestEac.SupplierEac ?? "-");
                }

                Tuple<string, string> route;
                var hasSupplier = SupportedSuppliers.Map.TryGetValue(x.SupplierName ?? "", out route);

                return new AccountMasterRowViewModel
                {
                    ContractId = x.Id,
                    EId = x.EId,
                    ContractType = x.ContractType,
                    SupplierId = x.SupplierId,
                    SupplierName = x.SupplierName ?? "-",
                    BusinessName = x.BusinessName ?? "-",
                    MPXN = x.MPXN ?? "-",
                    InputEAC = string.IsNullOrEmpty(x.InputEAC) ? "-" : x.InputEAC,
                    SupplierEAC = supplierEACDisplay,
                    InputDate = x.InputDate,
                    StartDate = x.StartDate,
                    CED = cr?.CED,
                    COTDate = cr?.CED_COT,
                    ContractStatus = status?.ContractStatus ?? "-",
                    PaymentStatus = status?.PaymentStatus ?? "-",
                    PreviousInvoiceNumbers = prevInvNumbers,
                    AccountsNotes = cr?.SupplierCobanaInvoiceNotes ?? "-",
                    CommissionForecast = metric?.InitialCommissionForecast,
                    CobanaDueCommission = cr?.CobanaDueCommission,
                    CobanaPaidCommission = cobanaPaidSum == 0 ? (decimal?)null : cobanaPaidSum,
                    CobanaReconciliation = cr?.CobanaFinalReconciliation,
                    // Edit Button Action and Controller ----- 
                    Controller = hasSupplier ? route.Item1 : SupportedSuppliers.DefaultController,
                    Action = hasSupplier ? route.Item2 : SupportedSuppliers.DefaultAction
                };

            }).ToList();

            return Json(new
            {
                draw = q.Draw,
                recordsTotal = total,
                recordsFiltered = total,
                data
            });
        }

    }
}