using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.PreSales;
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
    [Authorize(Roles = "Pre-sales")]
    public class PreSalesCalendarController : BaseController
    {
        private readonly ApplicationDBContext db;

        public PreSalesCalendarController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Views/PreSales/PreSalesCalendar.cshtml");
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesCalendarContracts()
        {
            try
            {
                int start = Convert.ToInt32(Request.Form["start"]);
                int length = Convert.ToInt32(Request.Form["length"]);
                string searchValue = Request.Form["search[value]"];
                string selectedDate = Request.Form["SelectedDate"];

                // Get Electric contracts with PreSalesFollowUpDate
                var electricQuery = db.CE_ElectricContracts
                    .Where(c => c.PreSalesFollowUpDate != null);

                // Get Gas contracts with PreSalesFollowUpDate
                var gasQuery = db.CE_GasContracts
                    .Where(c => c.PreSalesFollowUpDate != null);

                if (!string.IsNullOrWhiteSpace(selectedDate))
                {
                    DateTime selectedDateTime;
                    if (DateTime.TryParse(selectedDate, out selectedDateTime))
                    {
                        electricQuery = electricQuery.Where(c => DbFunctions.TruncateTime(c.PreSalesFollowUpDate.Value) == selectedDateTime.Date);
                        gasQuery = gasQuery.Where(c => DbFunctions.TruncateTime(c.PreSalesFollowUpDate.Value) == selectedDateTime.Date);
                    }
                }

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    electricQuery = electricQuery.Where(c =>
                        c.EId.Contains(searchValue) ||
                        c.MPAN.Contains(searchValue) ||
                        c.PostCode.Contains(searchValue) ||
                        c.BusinessName.Contains(searchValue) ||
                        c.CurrentSupplier.Contains(searchValue) ||
                        c.ContractNotes.Contains(searchValue));
                }

                // Get total counts
                int electricTotal = await electricQuery.CountAsync();
                int gasTotal = await gasQuery.CountAsync();
                int totalRecords = electricTotal + gasTotal;

                // Get paginated results
                var electricContracts = await electricQuery
                    .OrderBy(c => c.PreSalesFollowUpDate)
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                // Process electric contracts with agent determination
                var processedElectricContracts = electricContracts.Select(c =>
                {
                    string agentName = "-";
                    
                    if (c.Department == "In House" && c.CloserId.HasValue)
                    {
                        var closer = db.CE_Sector
                            .Where(s => s.SectorID == c.CloserId && s.SectorType == "closer")
                            .Select(s => s.Name)
                            .FirstOrDefault();
                        agentName = closer ?? "-";
                    }
                    else if (c.Department == "Brokers" && c.BrokerageStaffId.HasValue)
                    {
                        var brokerageStaff = db.CE_BrokerageStaff
                            .Where(bs => bs.BrokerageStaffID == c.BrokerageStaffId)
                            .Select(bs => bs.BrokerageStaffName)
                            .FirstOrDefault();
                        agentName = brokerageStaff ?? "-";
                    }
                    else if (c.Department == "Introducers" && c.SubIntroducerId.HasValue)
                    {
                        var subIntroducer = db.CE_SubIntroducer
                            .Where(si => si.SubIntroducerID == c.SubIntroducerId)
                            .Select(si => si.SubIntroducerName)
                            .FirstOrDefault();
                        agentName = subIntroducer ?? "-";
                    }

                    return new PreSalesCalendarRowViewModel
                    {
                        EId = c.EId,
                        Agent = agentName,
                        MPAN = c.MPAN,
                        MPRN = "",
                        PostCode = c.PostCode,
                        BusinessName = c.BusinessName,
                        Supplier = c.CurrentSupplier,
                        Type = "Electric",
                        PreSalesFollowUpDate = c.PreSalesFollowUpDate
                    };
                }).ToList();

                // If we need more records and haven't reached the end of electric contracts
                var remainingLength = length - electricContracts.Count;
                if (remainingLength > 0)
                {
                    var gasContracts = await gasQuery
                        .OrderBy(c => c.PreSalesFollowUpDate)
                        .Skip(Math.Max(0, start - electricTotal))
                        .Take(remainingLength)
                        .ToListAsync();

                    // Process gas contracts with agent determination
                    var processedGasContracts = gasContracts.Select(c =>
                    {
                        string agentName = "-";
                        
                        if (c.Department == "In House" && c.CloserId.HasValue)
                        {
                            var closer = db.CE_Sector
                                .Where(s => s.SectorID == c.CloserId && s.SectorType == "closer")
                                .Select(s => s.Name)
                                .FirstOrDefault();
                            agentName = closer ?? "-";
                        }
                        else if (c.Department == "Brokers" && c.BrokerageStaffId.HasValue)
                        {
                            var brokerageStaff = db.CE_BrokerageStaff
                                .Where(bs => bs.BrokerageStaffID == c.BrokerageStaffId)
                                .Select(bs => bs.BrokerageStaffName)
                                .FirstOrDefault();
                            agentName = brokerageStaff ?? "-";
                        }
                        else if (c.Department == "Introducers" && c.SubIntroducerId.HasValue)
                        {
                            var subIntroducer = db.CE_SubIntroducer
                                .Where(si => si.SubIntroducerID == c.SubIntroducerId)
                                .Select(si => si.SubIntroducerName)
                                .FirstOrDefault();
                            agentName = subIntroducer ?? "-";
                        }

                        return new PreSalesCalendarRowViewModel
                        {
                            EId = c.EId,
                            Agent = agentName,
                            MPAN = "",
                            MPRN = c.MPRN,
                            PostCode = c.PostCode,
                            BusinessName = c.BusinessName,
                            Supplier = c.CurrentSupplier,
                            Type = "Gas",
                            PreSalesFollowUpDate = c.PreSalesFollowUpDate
                        };
                    }).ToList();

                    processedElectricContracts.AddRange(processedGasContracts);
                }

                return Json(new
                {
                    draw = Request.Form["draw"],
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = processedElectricContracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesCalendarContracts: " + ex);
                return JsonResponse.Fail("Error fetching pre-sales calendar contracts.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesFollowUpDates()
        {
            try
            {
                // Get unique dates from Electric contracts
                var electricDates = await db.CE_ElectricContracts
                    .Where(c => c.PreSalesFollowUpDate != null)
                    .Select(c => c.PreSalesFollowUpDate.Value)
                    .Distinct()
                    .ToListAsync();

                // Get unique dates from Gas contracts
                var gasDates = await db.CE_GasContracts
                    .Where(c => c.PreSalesFollowUpDate != null)
                    .Select(c => c.PreSalesFollowUpDate.Value)
                    .Distinct()
                    .ToListAsync();

                // Combine and format dates
                var allDates = electricDates.Union(gasDates)
                    .Select(d => d.ToString("yyyy-MM-dd"))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                return JsonResponse.Ok(allDates);
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesFollowUpDates error: " + ex);
                return JsonResponse.Fail("Could not fetch pre-sales follow-up dates.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetContractNotes(string eId, string contractType)
        {
            try
            {
                var model = new EditContractNotesPopupViewModel
                {
                    EId = eId,
                    ContractType = contractType,
                    ContractNotes = "",
                    PreSalesFollowUpDate = ""
                };

                if (contractType == "Electric")
                {
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        model.ContractNotes = electricContract.ContractNotes ?? "";
                        model.PreSalesFollowUpDate = electricContract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd") ?? "";
                    }
                }
                else if (contractType == "Gas")
                {
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (gasContract != null)
                    {
                        model.ContractNotes = gasContract.ContractNotes ?? "";
                        model.PreSalesFollowUpDate = gasContract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd") ?? "";
                    }
                }

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractNotes error: " + ex);
                return JsonResponse.Fail("Could not fetch contract notes.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateContractNotes(EditContractNotesPopupViewModel model)
        {
            try
            {
                if (model.ContractType == "Electric")
                {
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        electricContract.ContractNotes = model.ContractNotes;
                        electricContract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                        await db.SaveChangesAsync();
                    }
                }
                else if (model.ContractType == "Gas")
                {
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (gasContract != null)
                    {
                        gasContract.ContractNotes = model.ContractNotes;
                        gasContract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                        await db.SaveChangesAsync();
                    }
                }

                return JsonResponse.Ok("Contract notes and follow-up date updated successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateContractNotes error: " + ex);
                return JsonResponse.Fail("Could not update contract notes.");
            }
        }
    }
}
