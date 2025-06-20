using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Gas;
using CobanaEnergy.Project.Models.Gas.EditGas;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
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
    public class GasController : BaseController
    {
        private readonly ApplicationDBContext db;

        public GasController()
        {
            db = new ApplicationDBContext();
        }

        #region create_gas

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateGas()
        {
            return View("CreateGas");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> CreateGas(GasContractCreateViewModel model)
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

                    var contract = new CE_GasContracts
                    {
                        EId = guid,
                        Agent = model.Agent,
                        Introducer = model.Introducer,
                        SubIntroducer = model.SubIntroducer,
                        MPRN = model.MPRN,
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
                        InputDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        InitialStartDate = model.InitialStartDate,
                        Uplift = model.Uplift.ToString(),
                        Duration = model.Duration,
                        InputEAC = model.InputEAC.ToString(),
                        UnitRate = model.UnitRate.ToString(),
                        OtherRate = model.OtherRate.ToString(),
                        StandingCharge = model.StandingCharge.ToString(),
                        //SortCode = model.SortCode,
                        // AccountNumber = model.AccountNumber,
                        CurrentSupplier = model.CurrentSupplier,
                        SupplierId = model.SupplierId,
                        ProductId = model.ProductId,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Gas",
                        Department = model.Department,
                        Source = model.Source,
                        SalesType = model.SalesType,
                        SalesTypeStatus = model.SalesTypeStatus,
                        SupplierCommsType = model.SupplierCommsType,
                        PreSalesStatus = model.PreSalesStatus
                    };

                    db.CE_GasContracts.Add(contract);
                    await db.SaveChangesAsync();

                    db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Gas",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Gas Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Gas contract created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("CreateGas failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while saving the gas contract.");
                }
            }
        }

        #endregion

        #region CheckDuplicateMprn

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateMprn(string mprn)
        {
            if (string.IsNullOrWhiteSpace(mprn) || mprn.Length < 6 || mprn.Length > 10 || !mprn.All(char.IsDigit))
                return JsonResponse.Fail("Invalid MPRN format");

            try
            {
                var match = await db.CE_GasContracts
                    .Where(c => c.MPRN == mprn)
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
                Logger.Log("CheckDuplicateMprn failed: " + ex.Message);
                return JsonResponse.Fail("Error occurred while checking MPRN.");
            }
        }

        #endregion

        #region Edit_Gas

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> EditGas(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();
            try
            {
                var contract = await db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == eid);
                var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Gas");

                if (contract == null)
                    return HttpNotFound();

                var model = new GasContractEditViewModel
                {
                    EId = contract.EId,
                    Agent = contract.Agent,
                    Introducer = contract.Introducer,
                    SubIntroducer = contract.SubIntroducer,
                    MPRN = contract.MPRN,
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
                    InputDate = contract.InputDate,
                    Uplift = Convert.ToDecimal(contract.Uplift),
                    Duration = contract.Duration,
                    InputEAC = Convert.ToDecimal(contract.InputEAC),
                    UnitRate = Convert.ToDecimal(contract.UnitRate),
                    OtherRate = Convert.ToDecimal(contract.OtherRate),
                    StandingCharge = Convert.ToDecimal(contract.StandingCharge),
                    SortCode = account?.SortCode ?? "",
                    AccountNumber = account?.AccountNumber ?? "",
                    CurrentSupplier = contract.CurrentSupplier,
                    SupplierId = contract.SupplierId,
                    ProductId = contract.ProductId,
                    EMProcessor = contract.EMProcessor,
                    ContractChecked = contract.ContractChecked,
                    ContractAudited = contract.ContractAudited,
                    Terminated = contract.Terminated,
                    ContractNotes = contract.ContractNotes,
                    Department = contract.Department,
                    Source = contract.Source,
                    SalesType = contract.SalesType,
                    SalesTypeStatus = contract.SalesTypeStatus,
                    SupplierCommsType = contract.SupplierCommsType,
                    PreSalesStatus = contract.PreSalesStatus
                };

                return View("EditGas", model);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to Load Gas Contract : " + ex.Message);
                return JsonResponse.Fail("Failed to Load Gas Contract");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditGas(GasContractEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var contract = await db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == model.EId);
                    var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Gas");

                    if (contract == null)
                        return JsonResponse.Fail("Contract not found.");

                    contract.Agent = model.Agent;
                    contract.Introducer = model.Introducer;
                    contract.SubIntroducer = model.SubIntroducer;
                    contract.MPRN = model.MPRN;
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
                    contract.UnitRate = model.UnitRate.ToString();
                    contract.OtherRate = model.OtherRate.ToString();
                    contract.StandingCharge = model.StandingCharge.ToString();
                    contract.CurrentSupplier = model.CurrentSupplier;
                    contract.SupplierId = model.SupplierId;
                    contract.ProductId = model.ProductId;
                    contract.EMProcessor = model.EMProcessor;
                    contract.ContractChecked = model.ContractChecked;
                    contract.ContractAudited = model.ContractAudited;
                    contract.Terminated = model.Terminated;
                    contract.ContractNotes = model.ContractNotes;
                    contract.Department = model.Department;
                    contract.Source = model.Source;
                    contract.SalesType = model.SalesType;
                    contract.SalesTypeStatus = model.SalesTypeStatus;
                    contract.SupplierCommsType = model.SupplierCommsType;
                    contract.PreSalesStatus = model.PreSalesStatus;
                    contract.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    var triggeringStatuses = new[]
                    {
                        "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted"
                    };

                    if (triggeringStatuses.Contains(model.PreSalesStatus))
                    {
                        contract.InputDate = DateTime.Now.ToString("dd/MM/yyyy");
                    }
                    model.InputDate = contract.InputDate;

                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = model.EId,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Gas Contract updated."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
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
                        contract.UnitRate,
                        contract.OtherRate,
                        contract.StandingCharge,
                        contract.MPRN,
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

                    }, "Gas contract updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("EditGas failed: " + ex.Message);
                    return JsonResponse.Fail("Error occurred while updating the gas contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForGasContract(string eid)
        {
            try
            {
                var logs = await db.CE_GasContractLogs
                    .Where(l => l.EId == eid)
                    .OrderByDescending(l => l.ActionDate)
                    .Select(l => new
                    {
                        l.Username,
                        l.ActionDate,
                        l.PreSalesStatusType,
                        l.Message
                    })
                    .ToListAsync();

                return JsonResponse.Ok(logs);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForGasContract failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve logs.");
            }
        }

        #endregion

    }
}