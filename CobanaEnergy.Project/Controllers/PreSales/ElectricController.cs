using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class ElectricController : BaseController
    {
        private readonly ApplicationDBContext db;
        public ElectricController()
        {
            db = new ApplicationDBContext();
        }

        #region create_electric

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateElectric()
        {
            return View("CreateElectric");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> CreateElectric(ElectricContractCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                             .Where(x => x.Value.Errors.Count > 0)
                             .SelectMany(kvp => kvp.Value.Errors)
                             .Select(e => e.ErrorMessage)
                             .ToList();

                string combinedError = string.Join("<br>", errors);
                return JsonResponse.Fail(combinedError);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var guid = Guid.NewGuid().ToString().ToUpper();

                    var contract = new CE_ElectricContracts
                    {
                        EId = guid,
                        Agent = model.Agent,
                        Introducer = model.Introducer,
                        SubIntroducer = model.SubIntroducer,
                        TopLine = model.TopLine,
                        MPAN = model.MPAN,
                        BusinessName = model.BusinessName,
                        CustomerName = model.CustomerName,
                        BusinessDoorNumber = model.BusinessDoorNumber,
                        BusinessHouseName = model.BusinessHouseName,
                        BusinessStreet = model.BusinessStreet,
                        BusinessTown = model.BusinessTown,
                        BusinessCounty = model.BusinessCounty,
                        PostCode = model.PostCode,
                        PhoneNumber1 = model.PhoneNumber1,
                        PhoneNumber2 = model.PhoneNumber2,
                        EmailAddress = model.EmailAddress,
                        InputDate = DateTime.Now.ToString("dd/MM/yyyy"),//changed
                        InitialStartDate = model.InitialStartDate,
                        Uplift = model.Uplift.ToString(),
                        Duration = model.Duration,
                        InputEAC = model.InputEAC.ToString(),
                        DayRate = model.DayRate.ToString(),
                        NightRate = model.NightRate.ToString(),
                        EveWeekendRate = model.EveWeekendRate.ToString(),
                        OtherRate = model.OtherRate.ToString(),
                        StandingCharge = model.StandingCharge.ToString(),
                        //SortCode = model.SortCode,
                        //AccountNumber = model.AccountNumber,
                        CurrentSupplier = model.CurrentSupplier,
                        SupplierId = model.SupplierId,
                        ProductId = model.ProductId,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        //LOGS = model.LOGS,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),//changed
                        UpdatedAt = "000",
                        Type = "Electric",
                        Department = model.Department,
                        Source = model.Source,
                        SalesType = model.SalesType,
                        SalesTypeStatus = model.SalesTypeStatus,
                        SupplierCommsType = model.SupplierCommsType,
                        PreSalesStatus = model.PreSalesStatus
                    };

                    db.CE_ElectricContracts.Add(contract);
                    await db.SaveChangesAsync();

                    db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Electric",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Electric Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    await db.SaveChangesAsync();

                    transaction.Commit();
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Electric contract created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("CreateElectric failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while saving the electric contract.");
                }
            }
        }

        #endregion

        #region CheckDuplicateMpan

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateMpan(string mpan)
        {
            if (string.IsNullOrWhiteSpace(mpan) || mpan.Length != 13 || !mpan.All(char.IsDigit))
                return JsonResponse.Fail("Invalid MPAN format");

            try
            {
                var match = await db.CE_ElectricContracts
                    .Where(c => c.MPAN == mpan)
                    .OrderByDescending(c => c.InputDate)
                    .FirstOrDefaultAsync();

                if (match != null)
                {
                    var result = new
                    {
                        match.Agent,
                        match.BusinessName,
                        match.CustomerName,
                        InputDate = DateTime.TryParse(match.InputDate, out DateTime parsedDate)
                                     ? parsedDate.ToString("dd/MM/yyyy")
                                     : match.InputDate,
                        match.PreSalesStatus,
                        match.Duration
                    };

                    return JsonResponse.Ok(result);
                }
                else
                {
                    return JsonResponse.Ok(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("CheckDuplicateMpan failed: " + ex.Message);
                return JsonResponse.Fail("Error occurred while checking MPAN.");
            }
        }

        #endregion

        #region electric_edit

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> ElectricEdit(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();
            try
            {
                var contract = await db.CE_ElectricContracts.FirstOrDefaultAsync(c => c.EId == eid);
                if (contract == null)
                    return HttpNotFound();

                var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Electric");

                var model = new ElectricContractEditViewModel
                {
                    EId = contract.EId,
                    Agent = contract.Agent,
                    Department = contract.Department,
                    Source = contract.Source,
                    Introducer = contract.Introducer,
                    SubIntroducer = contract.SubIntroducer,
                    SalesType = contract.SalesType,
                    SalesTypeStatus = contract.SalesTypeStatus,
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
                    Duration = contract.Duration,
                    Uplift = Convert.ToDecimal(contract.Uplift),
                    InputEAC = Convert.ToDecimal(contract.InputEAC),
                    DayRate = Convert.ToDecimal(contract.DayRate),
                    NightRate = Convert.ToDecimal(contract.NightRate),
                    EveWeekendRate = Convert.ToDecimal(contract.EveWeekendRate),
                    OtherRate = Convert.ToDecimal(contract.OtherRate),
                    StandingCharge = Convert.ToDecimal(contract.StandingCharge),
                    SortCode = account?.SortCode,
                    AccountNumber = account?.AccountNumber,
                    TopLine = contract.TopLine,
                    MPAN = contract.MPAN,
                    CurrentSupplier = contract.CurrentSupplier,
                    SupplierId = contract.SupplierId,
                    ProductId = contract.ProductId,
                    SupplierCommsType = contract.SupplierCommsType,
                    PreSalesStatus = contract.PreSalesStatus,
                    EMProcessor = contract.EMProcessor,
                    ContractChecked = contract.ContractChecked,
                    ContractAudited = contract.ContractAudited,
                    Terminated = contract.Terminated,
                    ContractNotes = contract.ContractNotes,
                    InputDate = contract.InputDate
                };

                return View("ElectricEdit", model);
            }
            catch (Exception ex)
            {
                Logger.Log("ElectricEdit failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred while fetching the contract.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> ElectricEdit(ElectricContractEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var contract = await db.CE_ElectricContracts.FirstOrDefaultAsync(c => c.EId == model.EId);
                    if (contract == null)
                        return JsonResponse.Fail("Electric Contract not found.");

                    contract.Agent = model.Agent;
                    contract.Department = model.Department;
                    contract.Source = model.Source;
                    contract.Introducer = model.Introducer;
                    contract.SubIntroducer = model.SubIntroducer;
                    contract.SalesType = model.SalesType;
                    contract.SalesTypeStatus = model.SalesTypeStatus;
                    contract.BusinessName = model.BusinessName;
                    contract.CustomerName = model.CustomerName;
                    contract.BusinessDoorNumber = model.BusinessDoorNumber;
                    contract.BusinessHouseName = model.BusinessHouseName;
                    contract.BusinessStreet = model.BusinessStreet;
                    contract.BusinessTown = model.BusinessTown;
                    contract.BusinessCounty = model.BusinessCounty;
                    contract.PostCode = model.PostCode;
                    contract.PhoneNumber1 = model.PhoneNumber1;
                    contract.PhoneNumber2 = model.PhoneNumber2;
                    contract.EmailAddress = model.EmailAddress;
                    contract.InitialStartDate = model.InitialStartDate;
                    contract.Duration = model.Duration;
                    contract.Uplift = model.Uplift.ToString();
                    contract.InputEAC = model.InputEAC.ToString();
                    contract.DayRate = model.DayRate.ToString();
                    contract.NightRate = model.NightRate.ToString();
                    contract.EveWeekendRate = model.EveWeekendRate.ToString();
                    contract.OtherRate = model.OtherRate.ToString();
                    contract.StandingCharge = model.StandingCharge.ToString();
                    contract.TopLine = model.TopLine;
                    contract.MPAN = model.MPAN;
                    contract.CurrentSupplier = model.CurrentSupplier;
                    contract.SupplierId = model.SupplierId;
                    contract.ProductId = model.ProductId;
                    contract.SupplierCommsType = model.SupplierCommsType;
                    contract.PreSalesStatus = model.PreSalesStatus;
                    contract.ContractChecked = model.ContractChecked;
                    contract.ContractAudited = model.ContractAudited;
                    contract.Terminated = model.Terminated;
                    contract.ContractNotes = model.ContractNotes;
                    contract.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    contract.EMProcessor = model.EMProcessor;

                    var triggeringStatuses = new[]
                    {
                        "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted"
                    };

                    if (triggeringStatuses.Contains(model.PreSalesStatus))
                    {
                        contract.InputDate = DateTime.Now.ToString("dd/MM/yyyy");
                    }

                    var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Electric");
                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = model.EId,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Electric contract updated."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    return JsonResponse.Ok(new
                    {
                        contract.Agent,
                        contract.Department,
                        contract.Source,
                        contract.Introducer,
                        contract.SubIntroducer,
                        contract.SalesType,
                        contract.SalesTypeStatus,
                        contract.BusinessName,
                        contract.CustomerName,
                        contract.BusinessDoorNumber,
                        contract.BusinessHouseName,
                        contract.BusinessStreet,
                        contract.BusinessTown,
                        contract.BusinessCounty,
                        contract.PostCode,
                        contract.PhoneNumber1,
                        contract.PhoneNumber2,
                        contract.EmailAddress,
                        contract.InitialStartDate,
                        contract.Duration,
                        contract.Uplift,
                        contract.InputEAC,
                        contract.DayRate,
                        contract.NightRate,
                        contract.EveWeekendRate,
                        contract.OtherRate,
                        contract.StandingCharge,
                        contract.TopLine,
                        contract.MPAN,
                        contract.CurrentSupplier,
                        contract.SupplierId,
                        contract.ProductId,
                        contract.SupplierCommsType,
                        contract.PreSalesStatus,
                        contract.ContractChecked,
                        contract.ContractAudited,
                        contract.Terminated,
                        contract.ContractNotes,
                        contract.EMProcessor,
                        contract.InputDate,
                        SortCode = account?.SortCode,
                        AccountNumber = account?.AccountNumber
                    }, "Electric contract updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("ElectricEdit failed: " + ex.ToString());
                    return JsonResponse.Fail("An error occurred while updating the contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForElectricContract(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return JsonResponse.Fail("Invalid Id.");

            try
            {
                var logs = await db.CE_ElectricContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new
                    {
                        l.Username,
                        l.ActionDate,
                        l.PreSalesStatusType,
                        l.Message
                    }).ToListAsync();

                var ordered = logs
                    .OrderByDescending(l => DateTime.TryParse(l.ActionDate, out var dt) ? dt : DateTime.MinValue)
                    .Select(l => new
                    {
                        l.Username,
                        l.ActionDate,
                        l.PreSalesStatusType,
                        l.Message
                    }).ToList();

                return JsonResponse.Ok(ordered);
            }
            catch (Exception ex)
            {
                Logger.Log("Error fetching logs: " + ex.ToString());
                return JsonResponse.Fail("Failed to fetch logs.");
            }
        }

        #endregion
    }
}