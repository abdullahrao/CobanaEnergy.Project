using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.CalendarDashboard;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.CalendarDashboard
{
    [Authorize(Roles = "Accounts,Controls")]
    public class CalendarDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public CalendarDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Views/Accounts/CalendarDashboard/CalendarDashboard.cshtml");
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetCalendarContracts()
        {
            try
            {
                int start = Convert.ToInt32(Request.Form["start"]);
                int length = Convert.ToInt32(Request.Form["length"]);
                string searchValue = Request.Form["search[value]"];
                string selectedDate = Request.Form["SelectedDate"];

                var query = db.CE_CommissionAndReconciliation
                    .Where(r => !string.IsNullOrEmpty(r.CommissionFollowUpDate));

                if (!string.IsNullOrWhiteSpace(selectedDate))
                {
                    query = query.Where(r => r.CommissionFollowUpDate == selectedDate);
                }

                int totalRecords = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    var matchingElectricIds = db.CE_ElectricContracts
                        .Where(c => c.InputDate.Contains(searchValue))
                        .Select(c => c.EId);

                    var matchingGasIds = db.CE_GasContracts
                        .Where(c => c.InputDate.Contains(searchValue))
                        .Select(c => c.EId);

                    query = query.Where(r =>
                        r.SupplierCobanaInvoiceNotes.Contains(searchValue) ||
                        r.EId.Contains(searchValue) ||
                        matchingElectricIds.Contains(r.EId) ||
                        matchingGasIds.Contains(r.EId) ||
                        db.CE_ElectricContracts.Any(c => c.EId == r.EId && c.ContractNotes.Contains(searchValue)) ||
                        db.CE_GasContracts.Any(c => c.EId == r.EId && c.ContractNotes.Contains(searchValue)) ||
                        db.CE_ContractStatuses.Any(cs => cs.EId == r.EId && cs.PaymentStatus.Contains(searchValue)) ||
                        db.CE_ContractStatuses.Any(cs => cs.EId == r.EId && cs.ContractStatus.Contains(searchValue)) ||
                        r.CommissionFollowUpDate.Contains(searchValue) ||
                        r.contractType.Contains(searchValue));
                }

                int filteredRecords = await query.CountAsync();

                var followUpRecords = await query
                    .OrderBy(r => r.CommissionFollowUpDate)
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                var contracts = new List<CalendarRowViewModel>();

                foreach (var record in followUpRecords)
                {
                    if (record.contractType == "Electric")
                    {
                        var electricContract = await db.CE_ElectricContracts
                            .FirstOrDefaultAsync(c => c.EId == record.EId);

                        if (electricContract != null)
                        {
                            var status = await db.CE_ContractStatuses
                                .FirstOrDefaultAsync(cs => cs.EId == electricContract.EId && cs.Type == "Electric");

                            contracts.Add(new CalendarRowViewModel
                            {
                                EId = electricContract.EId,
                                InputDate = electricContract.InputDate,
                                PaymentStatus = status?.PaymentStatus ?? "N/A",
                                ContractStatus = status?.ContractStatus ?? "N/A",
                                PreSalesNotes = electricContract.ContractNotes ?? "N/A",
                                SupplierCobanaInvoiceNotes = record.SupplierCobanaInvoiceNotes ?? "N/A",
                                Type = "Electric"
                            });
                        }
                    }
                    else if (record.contractType == "Gas")
                    {
                        var gasContract = await db.CE_GasContracts
                            .FirstOrDefaultAsync(c => c.EId == record.EId);

                        if (gasContract != null)
                        {
                            var status = await db.CE_ContractStatuses
                                .FirstOrDefaultAsync(cs => cs.EId == gasContract.EId && cs.Type == "Gas");

                            contracts.Add(new CalendarRowViewModel
                            {
                                EId = gasContract.EId,
                                InputDate = gasContract.InputDate,
                                PaymentStatus = status?.PaymentStatus ?? "N/A",
                                ContractStatus = status?.ContractStatus ?? "N/A",
                                PreSalesNotes = gasContract.ContractNotes ?? "N/A",
                                SupplierCobanaInvoiceNotes = record.SupplierCobanaInvoiceNotes ?? "N/A",
                                Type = "Gas"
                            });
                        }
                    }
                }

                return Json(new
                {
                    draw = Request.Form["draw"],
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = contracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetCalendarContracts: " + ex);
                return JsonResponse.Fail("Error fetching calendar contracts.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetFollowUpDates()
        {
            try
            {
                var dateList = await db.CE_CommissionAndReconciliation
                    .Where(r => !string.IsNullOrEmpty(r.CommissionFollowUpDate))
                    .Select(r => r.CommissionFollowUpDate.Trim())
                    .Distinct()
                    .ToListAsync();

                var formatted = dateList
                    .Select(d =>
                    {
                        DateTime parsed;
                        return DateTime.TryParse(d, out parsed)
                            ? parsed.ToString("yyyy-MM-dd")
                            : null;
                    })
                    .Where(x => x != null)
                    .ToList();

                return JsonResponse.Ok(formatted); // ✅ Final Fix
            }
            catch (Exception ex)
            {
                Logger.Log("GetFollowUpDates error: " + ex);
                return JsonResponse.Fail("Could not fetch follow-up dates.");
            }
        }

    }
}