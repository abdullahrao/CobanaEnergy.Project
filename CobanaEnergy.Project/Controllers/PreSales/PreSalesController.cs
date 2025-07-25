using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Supplier.Active_Suppliers;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class PreSalesController : BaseController
    {
        private readonly ApplicationDBContext db;
        public PreSalesController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region contract_listing

        [Authorize(Roles = "Pre-sales,Controls")]
        [HttpGet]
        public ActionResult Dashboard()
        {
            return View("Dashboard");
        }

        [Authorize(Roles = "Pre-sales,Controls")]
        [HttpGet]
        public async Task<JsonResult> GetAllContracts()
        {
            try
            {
                var excludeStatuses = new List<string>
        {
            "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted",
            "Rejected Contract", "Failed Audit Call", "Failed Credit Check", "New Connection Failed Credit",
            "Meter Registration Failed Credit", "Incorrect Prices", "Incorrect Rejected Recording",
            "Meter not Supported (De-Energised)", "Duplicate Contract"
        };

                var electricContracts = await db.CE_ElectricContracts
                    .Select(e => new
                    {
                        e.EId,
                        e.Agent,
                        e.BusinessName,
                        e.CustomerName,
                        InputDate = e.InputDate,
                        PreSalesStatus = e.PreSalesStatus,
                        e.ContractNotes,
                        MPAN = e.MPAN,
                        MPRN = "",
                        Type = e.Type
                    }).ToListAsync();

                var gasContracts = await db.CE_GasContracts
                    .Select(g => new
                    {
                        g.EId,
                        g.Agent,
                        g.BusinessName,
                        g.CustomerName,
                        InputDate = g.InputDate,
                        PreSalesStatus = g.PreSalesStatus,
                        g.ContractNotes,
                        MPAN = "",
                        MPRN = g.MPRN,
                        Type = g.Type
                    }).ToListAsync();

                var combined = electricContracts
                    .Concat(gasContracts)
                    .GroupBy(x => x.EId)
                    .Select(g =>
                    {
                        var all = g.ToList();

                        var elec = all.FirstOrDefault(x => (x.Type == "Electric" || x.Type == "Dual") && !string.IsNullOrEmpty(x.MPAN));
                        var gas = all.FirstOrDefault(x => (x.Type == "Gas" || x.Type == "Dual") && !string.IsNullOrEmpty(x.MPRN));

                        bool isDual = elec != null && gas != null;

                        bool elecExcluded = elec != null && excludeStatuses.Contains((elec.PreSalesStatus ?? "").Trim());
                        bool gasExcluded = gas != null && excludeStatuses.Contains((gas.PreSalesStatus ?? "").Trim());

                        if (!isDual && elec != null && elecExcluded)
                            return null;

                        if (!isDual && gas != null && gasExcluded)
                            return null;

                        if (isDual && elecExcluded && gasExcluded)
                            return null;

                        string mergedStatus;
                        var elecsafeStatus = System.Net.WebUtility.HtmlEncode(elec?.PreSalesStatus);
                        var gassafeStatus = System.Net.WebUtility.HtmlEncode(gas?.PreSalesStatus);
                        if (isDual)
                        {
                            if (elecExcluded && !gasExcluded)
                            {
                                mergedStatus = $"<span class='highlight-red'>Electric: {elecsafeStatus}</span><br>Gas: {gassafeStatus}";
                            }
                            else if (gasExcluded && !elecExcluded)
                            {
                                mergedStatus = $"Electric: {elecsafeStatus}<br><span class='highlight-red'>Gas: {gassafeStatus}</span>";
                            }
                            else
                            {
                                mergedStatus = $"Electric: {elecsafeStatus}<br>Gas: {gassafeStatus}";
                            }
                        }
                        else
                        {
                            if (elec != null)
                                mergedStatus = elecExcluded ? $"<span class='highlight-red'>{elecsafeStatus}</span>" : elecsafeStatus;
                            else if (gas != null)
                                mergedStatus = gasExcluded ? $"<span class='highlight-red'>{gassafeStatus}</span>" : gassafeStatus;
                            else
                                mergedStatus = "-";
                        }

                        string mergedInputDate = isDual
                            ? string.Join("<br>", new[]
                            {
                        elec != null ? $"Electric: {elec.InputDate}" : null,
                        gas != null ? $"Gas: {gas.InputDate}" : null
                            }.Where(x => !string.IsNullOrWhiteSpace(x)))
                            : (elec?.InputDate ?? gas?.InputDate ?? "-");

                        DateTime elecDate = DateTime.TryParse(elec?.InputDate, out var eDate) ? eDate : DateTime.MinValue;
                        DateTime gasDate = DateTime.TryParse(gas?.InputDate, out var gDate) ? gDate : DateTime.MinValue;
                        DateTime sortable = elecDate > gasDate ? elecDate : gasDate;

                        return new
                        {
                            EId = g.Key,
                            Agent = elec?.Agent ?? gas?.Agent ?? "-",
                            MPAN = !string.IsNullOrWhiteSpace(elec?.MPAN) && elec.MPAN != "N/A" ? elec.MPAN : "N/A",
                            MPRN = !string.IsNullOrWhiteSpace(gas?.MPRN) && gas.MPRN != "N/A" ? gas.MPRN : "N/A",
                            BusinessName = elec?.BusinessName ?? gas?.BusinessName ?? "-",
                            CustomerName = elec?.CustomerName ?? gas?.CustomerName ?? "-",
                            InputDate = mergedInputDate,
                            PreSalesStatus = mergedStatus,
                            Notes = elec?.ContractNotes ?? gas?.ContractNotes ?? "-----",
                            Type = isDual ? "Dual" : elec != null ? "Electric" : "Gas",
                            SortableDate = sortable
                        };
                    })
                    .Where(x => x != null)
                    .OrderByDescending(x => x.SortableDate)
                    .ToList();

                return JsonResponse.Ok(combined);
            }
            catch (Exception ex)
            {
                Logger.Log("GetAllContracts: " + ex);
                return JsonResponse.Fail("Failed to fetch contract records.");
            }
        }

        #endregion

        #region active_suppliers

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetSupplierContractStats()
        {
            try
            {
                var todayStart = DateTime.Today.AddHours(8);
                var todayEnd = DateTime.Today.AddHours(19);
                var todayPrefix = DateTime.Today.ToString("dd/MM/yyyy ");

                var suppliers = await db.CE_Supplier.ToListAsync();

                var electricRaw = await db.CE_ElectricContracts
                    .Where(e => e.CreatedAt != null && e.CreatedAt.StartsWith(todayPrefix))
                    .ToListAsync();

                var gasRaw = await db.CE_GasContracts
                    .Where(g => g.CreatedAt != null && g.CreatedAt.StartsWith(todayPrefix))
                    .ToListAsync();


                var electricFiltered = electricRaw
                    .Where(e =>
                        DateTime.TryParse(e.CreatedAt, out var dt) &&
                        dt >= todayStart && dt <= todayEnd &&
                        e.SupplierId != 0)
                    .Select(e => new
                    {
                        SupplierId = e.SupplierId,
                        PreSalesStatus = e.PreSalesStatus,
                        Type = "Electric"
                    });

                var gasFiltered = gasRaw
                    .Where(g =>
                        DateTime.TryParse(g.CreatedAt, out var dt) &&
                        dt >= todayStart && dt <= todayEnd &&
                        g.SupplierId != 0)
                    .Select(g => new
                    {
                        SupplierId = g.SupplierId,
                        PreSalesStatus = g.PreSalesStatus,
                        Type = "Gas"
                    });

                var combined = electricFiltered
                    .Concat(gasFiltered)
                    .GroupBy(x => new { x.SupplierId, x.PreSalesStatus })
                    .Select(group =>
                    {
                        var supplier = suppliers.FirstOrDefault(s => s.Id == group.Key.SupplierId);
                        return new SupplierContractStatViewModel
                        {
                            SupplierName = supplier?.Name ?? "Unknown",
                            PreSalesStatus = group.Key.PreSalesStatus ?? "-",
                            ElectricCount = group.Count(x => x.Type == "Electric"),
                            GasCount = group.Count(x => x.Type == "Gas")
                        };
                    })
                    .OrderBy(x => x.SupplierName)
                    .ThenBy(x => x.PreSalesStatus)
                    .ToList();

                return JsonResponse.Ok(combined);
            }
            catch (Exception ex)
            {
                Logger.Log("GetSupplierContractStats: " + ex);
                return JsonResponse.Fail("Failed to fetch supplier stats.");
            }
        }

        #endregion

    }
}