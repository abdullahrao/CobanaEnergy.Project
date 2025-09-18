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
    public class PreSalesMasterDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public PreSalesMasterDashboardController(ApplicationDBContext _db)
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

            var model = new PreSalesMasterDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/PreSales/PreSalesMasterDashboard.cshtml", model);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesMasterContracts()
        {
            try
            {
                int start = Convert.ToInt32(Request.Form["start"]);
                int length = Convert.ToInt32(Request.Form["length"]);
                string searchValue = Request.Form["search[value]"];
                string inputDateFrom = Request.Form["inputDateFrom"];
                string inputDateTo = Request.Form["inputDateTo"];
                string supplierId = Request.Form["supplierId"];

                // Parse date filters
                DateTime? fromDate = null;
                DateTime? toDate = null;
                
                if (!string.IsNullOrWhiteSpace(inputDateFrom) && DateTime.TryParse(inputDateFrom, out DateTime from))
                    fromDate = from.Date;
                
                if (!string.IsNullOrWhiteSpace(inputDateTo) && DateTime.TryParse(inputDateTo, out DateTime to))
                    toDate = to.Date.AddDays(1).AddTicks(-1); // End of day

                // Parse supplier filter
                long? supplierFilter = null;
                if (!string.IsNullOrWhiteSpace(supplierId) && long.TryParse(supplierId, out long supplierIdLong))
                    supplierFilter = supplierIdLong;

                // Get Electric contracts
                var electricQuery = db.CE_ElectricContracts.AsQueryable();
                
                // Get Gas contracts
                var gasQuery = db.CE_GasContracts.AsQueryable();

                // Apply supplier filter
                if (supplierFilter.HasValue)
                {
                    electricQuery = electricQuery.Where(c => c.SupplierId == supplierFilter.Value);
                    gasQuery = gasQuery.Where(c => c.SupplierId == supplierFilter.Value);
                }

                // Get all contracts for processing
                var electricContracts = await electricQuery.ToListAsync();
                var gasContracts = await gasQuery.ToListAsync();

                // Apply date range filter after loading data
                if (fromDate.HasValue && toDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;
                    var end = toDate.Value.Date;

                    electricContracts = electricContracts
                        .Where(e => DateTime.TryParse(e.InputDate, out var dt) &&
                                    dt.Date >= startDate && dt.Date <= end)
                        .ToList();

                    gasContracts = gasContracts
                        .Where(g => DateTime.TryParse(g.InputDate, out var dt) &&
                                    dt.Date >= startDate && dt.Date <= end)
                        .ToList();
                }

                // Combine and process contracts for dual handling
                var combinedContracts = await ProcessCombinedContracts(electricContracts, gasContracts);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    combinedContracts = combinedContracts.Where(c =>
                        c.BusinessName.Contains(searchValue) ||
                        c.CustomerName.Contains(searchValue) ||
                        c.PostCode.Contains(searchValue) ||
                        c.Email.Contains(searchValue) ||
                        c.Supplier.Contains(searchValue) ||
                        c.ContractNotes.Contains(searchValue) ||
                        c.MPAN.Contains(searchValue) ||
                        c.MPRN.Contains(searchValue)).ToList();
                }

                // Get total count
                int totalRecords = combinedContracts.Count;

                // Apply pagination
                var paginatedContracts = combinedContracts
                    .OrderByDescending(c => c.SortableDate)
                    .Skip(start)
                    .Take(length)
                    .ToList();

                return Json(new
                {
                    draw = Request.Form["draw"],
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = paginatedContracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesMasterContracts: " + ex);
                return JsonResponse.Fail("Error fetching pre-sales master contracts.");
            }
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetContractNotesOnly(string eId, string contractType)
        {
            try
            {
                var model = new EditContractNotesOnlyViewModel
                {
                    EId = eId,
                    ContractType = contractType,
                    ContractNotes = "",
                    IsDualContract = false
                };

                if (contractType == "Electric")
                {
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        model.ContractNotes = electricContract.ContractNotes ?? "";

                        // Check if it is also a dual contract
                        var gasContract = await db.CE_GasContracts.Where(c => c.EId == eId).FirstOrDefaultAsync();
                        model.IsDualContract = gasContract != null;
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

                        // Check if it is also a dual contract
                        var electricalContract = await db.CE_ElectricContracts.Where(c => c.EId == eId).FirstOrDefaultAsync();
                        model.IsDualContract = electricalContract != null;
                    }
                }
                else if (contractType == "Dual")
                {
                    // For dual contracts, get data from both electric and gas
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();
                    
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        model.ContractNotes = electricContract.ContractNotes ?? "";
                        model.IsDualContract = true;
                    }
                }

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractNotesOnly error: " + ex);
                return JsonResponse.Fail("Could not fetch contract notes.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateContractNotesOnly(EditContractNotesOnlyViewModel model)
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
                        await db.SaveChangesAsync();
                    }
                }
                else if (model.ContractType == "Dual")
                {
                    // Update both electric and gas contracts for dual
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();
                    
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        electricContract.ContractNotes = model.ContractNotes;
                    }

                    if (gasContract != null)
                    {
                        gasContract.ContractNotes = model.ContractNotes;
                    }

                    if (electricContract != null || gasContract != null)
                    {
                        await db.SaveChangesAsync();
                    }
                }

                return JsonResponse.Ok("Contract notes updated successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateContractNotes error: " + ex);
                return JsonResponse.Fail("Could not update contract notes.");
            }
        }

        private async Task<List<PreSalesMasterDashboardRowViewModel>> ProcessCombinedContracts(List<Models.Electric.ElectricDBModels.CE_ElectricContracts> electricContracts, List<Models.Gas.GasDBModels.CE_GasContracts> gasContracts)
        {
            var result = new List<PreSalesMasterDashboardRowViewModel>();

            // Group by EId to handle dual contracts
            var allEIds = electricContracts.Select(e => e.EId).Union(gasContracts.Select(g => g.EId)).Distinct().ToList();

            foreach (var eId in allEIds)
            {
                var electricContract = electricContracts.FirstOrDefault(e => e.EId == eId);
                var gasContract = gasContracts.FirstOrDefault(g => g.EId == eId);

                bool isDual = electricContract != null && gasContract != null;

                if (isDual)
                {
                    // Create dual contract entry
                    var dualContract = await CreateDualContractViewModel(electricContract, gasContract);
                    result.Add(dualContract);
                }
                else
                {
                    // Create single contract entry
                    if (electricContract != null)
                    {
                        var electricViewModel = await CreateSingleContractViewModel(electricContract, "Electric");
                        result.Add(electricViewModel);
                    }
                    if (gasContract != null)
                    {
                        var gasViewModel = await CreateSingleContractViewModel(gasContract, "Gas");
                        result.Add(gasViewModel);
                    }
                }
            }

            return result;
        }

        private async Task<PreSalesMasterDashboardRowViewModel> CreateDualContractViewModel(Models.Electric.ElectricDBModels.CE_ElectricContracts electricContract, Models.Gas.GasDBModels.CE_GasContracts gasContract)
        {
            // Get agent information (use electric contract data as primary)
            string agentName = await GetAgentName(electricContract.Department, electricContract.CloserId, electricContract.BrokerageStaffId, electricContract.SubIntroducerId);

            // Get supplier names
            string electricSupplier = await GetSupplierName(electricContract.SupplierId);
            string gasSupplier = await GetSupplierName(gasContract.SupplierId);

            // Format dates
            string electricInputDate = electricContract.InputDate ?? "N/A";
            string gasInputDate = gasContract.InputDate ?? "N/A";

            // Parse dates for sorting
            DateTime electricDate = DateTime.TryParse(electricInputDate, out DateTime eDate) ? eDate : DateTime.MinValue;
            DateTime gasDate = DateTime.TryParse(gasInputDate, out DateTime gDate) ? gDate : DateTime.MinValue;
            DateTime sortableDate = electricDate > gasDate ? electricDate : gasDate;

            return new PreSalesMasterDashboardRowViewModel
            {
                EId = electricContract.EId,
                Agent = agentName,
                BusinessName = electricContract.BusinessName ?? gasContract.BusinessName ?? "N/A",
                CustomerName = electricContract.CustomerName ?? gasContract.CustomerName ?? "N/A",
                PostCode = electricContract.PostCode ?? gasContract.PostCode ?? "N/A",
                InputDate = $"Electric: {electricInputDate}<br>Gas: {gasInputDate}",
                Duration = $"Electric: {electricContract.Duration ?? "N/A"}<br>Gas: {gasContract.Duration ?? "N/A"}",
                Uplift = $"Electric: {electricContract.Uplift ?? "N/A"}<br>Gas: {gasContract.Uplift ?? "N/A"}",
                InputEAC = $"Electric: {electricContract.InputEAC ?? "N/A"}<br>Gas: {gasContract.InputEAC ?? "N/A"}",
                Email = electricContract.EmailAddress ?? gasContract.EmailAddress ?? "N/A",
                Supplier = $"Electric: {electricSupplier}<br>Gas: {gasSupplier}",
                ContractNotes = electricContract.ContractNotes ?? gasContract.ContractNotes ?? "N/A",
                Type = "Dual",
                SortableDate = sortableDate,
                MPAN = electricContract.MPAN ?? "N/A",
                MPRN = gasContract.MPRN ?? "N/A"
            };
        }

        private async Task<PreSalesMasterDashboardRowViewModel> CreateSingleContractViewModel(dynamic contract, string contractType)
        {
            string agentName = await GetAgentName(contract.Department, contract.CloserId, contract.BrokerageStaffId, contract.SubIntroducerId);
            string supplierName = await GetSupplierName(contract.SupplierId);
            string inputDate = contract.InputDate ?? "N/A";

            DateTime sortableDate = DateTime.TryParse(inputDate, out DateTime date) ? date : DateTime.MinValue;

            return new PreSalesMasterDashboardRowViewModel
            {
                EId = contract.EId,
                Agent = agentName,
                BusinessName = contract.BusinessName ?? "N/A",
                CustomerName = contract.CustomerName ?? "N/A",
                PostCode = contract.PostCode ?? "N/A",
                InputDate = inputDate,
                Duration = contract.Duration ?? "N/A",
                Uplift = contract.Uplift ?? "N/A",
                InputEAC = contract.InputEAC ?? "N/A",
                Email = contract.EmailAddress ?? "N/A",
                Supplier = supplierName,
                ContractNotes = contract.ContractNotes ?? "N/A",
                Type = contractType,
                SortableDate = sortableDate,
                MPAN = contractType == "Electric" ? (contract.MPAN ?? "N/A") : "N/A",
                MPRN = contractType == "Gas" ? (contract.MPRN ?? "N/A") : "N/A"
            };
        }

        private async Task<string> GetAgentName(string department, int? closerId, int? brokerageStaffId, int? subIntroducerId)
        {
            if (department == "In House" && closerId.HasValue)
            {
                var closer = await db.CE_Sector
                    .Where(s => s.SectorID == closerId && s.SectorType == "closer")
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
                return closer ?? "-";
            }
            else if (department == "Brokers" && brokerageStaffId.HasValue)
            {
                var brokerageStaff = await db.CE_BrokerageStaff
                    .Where(bs => bs.BrokerageStaffID == brokerageStaffId)
                    .Select(bs => bs.BrokerageStaffName)
                    .FirstOrDefaultAsync();
                return brokerageStaff ?? "-";
            }
            else if (department == "Introducers" && subIntroducerId.HasValue)
            {
                var subIntroducer = await db.CE_SubIntroducer
                    .Where(si => si.SubIntroducerID == subIntroducerId)
                    .Select(si => si.SubIntroducerName)
                    .FirstOrDefaultAsync();
                return subIntroducer ?? "-";
            }

            return "-";
        }

        private async Task<string> GetSupplierName(long? supplierId)
        {
            if (!supplierId.HasValue) return "N/A";

            var supplier = await db.CE_Supplier
                .Where(s => s.Id == supplierId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            return supplier ?? "N/A";
        }
    }
}
