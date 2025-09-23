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
            // ELECTRIC contracts
            var electric = from ec in _db.CE_ElectricContracts
                           join sp in _db.CE_Supplier on ec.SupplierId equals sp.Id
                           select new ContractViewModel
                           {
                               Id = ec.Id.ToString(),
                               EId = ec.EId,
                               ContractType = "Electric",
                               BusinessName = ec.BusinessName,
                               SupplierId = ec.SupplierId,
                               SupplierName = sp.Name,
                               Department = ec.Department,
                               MPXN = ec.MPAN,
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

            // GAS contracts
            var gas = from gc in _db.CE_GasContracts
                      join sp in _db.CE_Supplier on gc.SupplierId equals sp.Id
                      select new ContractViewModel
                      {
                          Id = gc.Id.ToString(),
                          EId = gc.EId,
                          ContractType = "Gas",
                          BusinessName = gc.BusinessName,
                          SupplierId = gc.SupplierId,
                          SupplierName = sp.Name,
                          Department = gc.Department,
                          MPXN = gc.MPRN,
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

            // Combine
            var combined = electric.Concat(gas);

            // --- Apply filters first ---
            if (!string.IsNullOrEmpty(q.Supplier))
            {
                if (int.TryParse(q.Supplier, out var sid))
                    combined = combined.Where(x => x.SupplierId == sid);
            }

            if (!string.IsNullOrEmpty(q.Department))
                combined = combined.Where(x => x.Department == q.Department);

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


            // Load data (ToListAsync once)
            var contracts = await combined.ToListAsync();

            var eIds = contracts.Select(x => x.EId).Distinct().ToList();

            // Related data (loaded once)
            var statuses = await _db.CE_ContractStatuses
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => s.EId)
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            // ContractStatus filter
            if (!string.IsNullOrEmpty(q.ContractStatus))
            {
                var statusMap = statuses
                    .Where(s => s != null && s.ContractStatus == q.ContractStatus)
                    .Select(s => s.EId)
                    .ToHashSet();

                contracts = contracts
                    .Where(x => statusMap.Contains(x.EId))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(q.PaymentStatus))
            {
                var statusMap = statuses
                    .Where(s => s != null && s.PaymentStatus == q.PaymentStatus)
                    .Select(s => s.EId)
                    .ToHashSet();

                contracts = contracts
                    .Where(x => statusMap.Contains(x.EId))
                    .ToList();
            }

            var crs = await _db.CE_CommissionAndReconciliation
                .Where(c => eIds.Contains(c.EId))
                .ToListAsync();

            var reconciliationIds = crs.Select(c => c.Id).Distinct().ToList();

            var metrics = await _db.CE_CommissionMetrics
                .Where(m => reconciliationIds.Contains(m.ReconciliationId))
                .ToListAsync();

            var eacGroups = await _db.CE_EacLogs
                .Where(l => eIds.Contains(l.EId))
                .GroupBy(l => l.EId)
                .Select(g => new
                {
                    EId = g.Key,
                    Latest = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault(),
                    All = g.ToList()
                })
                .ToListAsync();

            // ---- Sorting ----
            var sortColumn = q.Columns?[q.Order?[0].Column ?? 0].Data;
            var sortDir = q.Order?[0].Dir?.ToLower();

            Func<ContractViewModel, object> keySelector = x => null;

            switch (sortColumn)
            {
                case "SupplierName": keySelector = x => x.SupplierName; break;
                case "BusinessName": keySelector = x => x.BusinessName; break;
                case "MPXN": keySelector = x => x.MPXN; break;
                case "InputEAC": keySelector = x => x.InputEAC; break;
                case "InputDate": keySelector = x => x.InputDate; break;
                case "StartDate": keySelector = x => x.StartDate; break;
                case "CED": keySelector = x => crs.FirstOrDefault(c => c.EId == x.EId)?.CED; break;
                case "COTDate": keySelector = x => crs.FirstOrDefault(c => c.EId == x.EId)?.CED_COT; break;
                case "ContractStatus": keySelector = x => statuses.FirstOrDefault(s => s.EId == x.EId)?.ContractStatus ?? ""; break;
                case "PaymentStatus": keySelector = x => statuses.FirstOrDefault(s => s.EId == x.EId)?.PaymentStatus ?? ""; break;
                case "CommissionForecast": keySelector = x => metrics.FirstOrDefault(m => m.ReconciliationId == crs.FirstOrDefault(c => c.EId == x.EId)?.Id)?.InitialCommissionForecast ?? "0"; break;
                case "CobanaPaidCommission":
                    keySelector = x =>
                    {
                        var eac = eacGroups.FirstOrDefault(g => g.EId == x.EId);
                        return eac?.All
                            .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceAmount))
                            .Select(i => decimal.TryParse(i.InvoiceAmount, out var val) ? val : 0)
                            .Sum() ?? 0m;
                    };
                    break;
            }

            if (keySelector != null)
            {
                contracts = (sortDir == "desc")
                    ? contracts.OrderByDescending(keySelector).ToList()
                    : contracts.OrderBy(keySelector).ToList();
            }

            // ---- Paging ----
            var total = contracts.Count;
            var page = contracts.Skip(q.Start).Take(q.Length).ToList();

            // ---- Map final rows ----
            var data = page.Select(x =>
            {
                var eacGroup = eacGroups.FirstOrDefault(e => e.EId == x.EId);
                var latestEac = eacGroup?.Latest;
                var status = statuses.FirstOrDefault(s => s.EId == x.EId);
                var cr = crs.FirstOrDefault(c => c.EId == x.EId);
                var metric = metrics.FirstOrDefault(m => m.ReconciliationId == cr?.Id);

                decimal cobanaPaidSum = 0m;
                if (eacGroup?.All.Any() == true)
                {
                    cobanaPaidSum = eacGroup.All
                        .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceAmount))
                        .Select(i => decimal.TryParse(i.InvoiceAmount, out var val) ? val : 0)
                        .Sum();
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
                    SupplierEAC = latestEac?.SupplierEac ?? "-",
                    InputDate = x.InputDate,
                    StartDate = x.StartDate,
                    CED = cr?.CED,
                    COTDate = cr?.CED_COT,
                    ContractStatus = status?.ContractStatus ?? "-",
                    PaymentStatus = status?.PaymentStatus ?? "-",
                    CommissionForecast = metric?.InitialCommissionForecast,
                    CobanaDueCommission = cr?.CobanaDueCommission,
                    CobanaPaidCommission = cobanaPaidSum == 0 ? (decimal?)null : cobanaPaidSum,
                    CobanaFinalReconciliation = cr?.CobanaFinalReconciliation,

                    // Edit Button Action and Controller ----- 
                    Controller = hasSupplier ? route.Item1 : SupportedSuppliers.DefaultController,
                    Action = hasSupplier ? route.Item2 : SupportedSuppliers.DefaultAction
                };
            });

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