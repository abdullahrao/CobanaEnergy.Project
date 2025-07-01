using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Dual;
using CobanaEnergy.Project.Models.Dual.EditDual;
using CobanaEnergy.Project.Models.Dual.LogsHelperClass;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class DualController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public DualController()
        {
            _db = new ApplicationDBContext();
        }

        #region Dual_Contract

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateDual()
        {
            return View("CreateDual");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> CreateDual(DualContractCreateViewModel model)
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

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var guid = Guid.NewGuid().ToString().ToUpper();

                    var electric = new CE_ElectricContracts
                    {
                        EId = guid,
                        Department = model.Department,
                        Agent = model.Agent,
                        Source = model.Source,
                        Introducer = model.Introducer,
                        SubIntroducer = model.SubIntroducer,
                        SalesType = model.ElectricSalesType,
                        SalesTypeStatus = model.ElectricSalesTypeStatus,
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
                        InitialStartDate = model.ElectricInitialStartDate,
                        Duration = model.ElectricDuration,
                        Uplift = model.ElectricUplift.ToString(),
                        InputEAC = model.ElectricInputEAC.ToString(),
                        DayRate = model.ElectricDayRate.ToString(),
                        NightRate = model.ElectricNightRate.ToString(),
                        EveWeekendRate = model.ElectricEveWeekendRate.ToString(),
                        OtherRate = model.ElectricOtherRate.ToString(),
                        StandingCharge = model.ElectricStandingCharge.ToString(),
                        TopLine = model.TopLine,
                        MPAN = model.MPAN,
                        CurrentSupplier = model.ElectricCurrentSupplier,
                        SupplierId = model.ElectricSupplierId,
                        ProductId = model.ElectricProductId,
                        SupplierCommsType = model.ElectricSupplierCommsType,
                        PreSalesStatus = model.ElectricPreSalesStatus,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Dual"
                    };

                    _db.CE_ElectricContracts.Add(electric);
                    await _db.SaveChangesAsync();

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.ElectricPreSalesStatus,
                        Message = "Dual Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    var gas = new CE_GasContracts
                    {
                        EId = guid,
                        Department = model.Department,
                        Agent = model.Agent,
                        Source = model.Source,
                        Introducer = model.Introducer,
                        SubIntroducer = model.SubIntroducer,
                        SalesType = model.GasSalesType,
                        SalesTypeStatus = model.GasSalesTypeStatus,
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
                        InitialStartDate = model.GasInitialStartDate,
                        Duration = model.GasDuration,
                        Uplift = model.GasUplift.ToString(),
                        InputEAC = model.GasInputEAC.ToString(),
                        UnitRate = model.GasUnitRate.ToString(),
                        OtherRate = model.GasOtherRate.ToString(),
                        StandingCharge = model.GasStandingCharge.ToString(),
                        MPRN = model.MPRN,
                        CurrentSupplier = model.GasCurrentSupplier,
                        SupplierId = model.GasSupplierId,
                        ProductId = model.GasProductId,
                        SupplierCommsType = model.GasSupplierCommsType,
                        PreSalesStatus = model.GasPreSalesStatus,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Dual"
                    };

                    _db.CE_GasContracts.Add(gas);
                    await _db.SaveChangesAsync();

                    _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.GasPreSalesStatus,
                        Message = "Dual Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    _db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Dual",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    await _db.SaveChangesAsync();
                    tx.Commit();

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Dual contract created successfully.");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Logger.Log("DualController.CreateDual: " + ex);
                    return JsonResponse.Fail("An unexpected error occurred while creating the dual contract.");
                }
            }
        }

        #endregion

        #region Edit_Dual

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> EditDual(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();

            try
            {
                var electric = await _db.CE_ElectricContracts.FirstOrDefaultAsync(e => e.EId == eid && e.Type == "Dual");
                var gas = await _db.CE_GasContracts.FirstOrDefaultAsync(g => g.EId == eid && g.Type == "Dual");
                var account = await _db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Dual");

                if (electric == null || gas == null)
                    return HttpNotFound();

                var model = new DualContractEditViewModel
                {
                    EId = eid,

                    // Shared
                    Department = electric.Department,
                    Agent = electric.Agent,
                    Source = electric.Source,
                    Introducer = electric.Introducer,
                    SubIntroducer = electric.SubIntroducer,
                    BusinessName = electric.BusinessName,
                    CustomerName = electric.CustomerName,
                    BusinessDoorNumber = electric.BusinessDoorNumber,
                    BusinessHouseName = electric.BusinessHouseName,
                    BusinessStreet = electric.BusinessStreet,
                    BusinessTown = electric.BusinessTown,
                    BusinessCounty = electric.BusinessCounty,
                    PostCode = electric.PostCode,
                    PhoneNumber1 = electric.PhoneNumber1,
                    PhoneNumber2 = electric.PhoneNumber2,
                    EmailAddress = electric.EmailAddress,
                    EMProcessor = electric.EMProcessor,
                    ContractChecked = electric.ContractChecked,
                    ContractAudited = electric.ContractAudited,
                    Terminated = electric.Terminated,
                    ContractNotes = electric.ContractNotes,
                    SortCode = account?.SortCode ?? "",
                    AccountNumber = account?.AccountNumber ?? "",
                    ElectricInputDate = electric.InputDate,

                    // Electric
                    TopLine = electric.TopLine,
                    MPAN = electric.MPAN,
                    ElectricCurrentSupplier = electric.CurrentSupplier,
                    ElectricSupplierId = electric.SupplierId,
                    ElectricProductId = electric.ProductId,
                    ElectricInitialStartDate = electric.InitialStartDate,
                    ElectricDuration = electric.Duration,
                    ElectricSalesType = electric.SalesType,
                    ElectricSalesTypeStatus = electric.SalesTypeStatus,
                    ElectricUplift = Convert.ToDecimal(electric.Uplift),
                    ElectricInputEAC = Convert.ToDecimal(electric.InputEAC),
                    ElectricDayRate = Convert.ToDecimal(electric.DayRate),
                    ElectricNightRate = Convert.ToDecimal(electric.NightRate),
                    ElectricEveWeekendRate = Convert.ToDecimal(electric.EveWeekendRate),
                    ElectricOtherRate = Convert.ToDecimal(electric.OtherRate),
                    ElectricStandingCharge = Convert.ToDecimal(electric.StandingCharge),
                    ElectricSupplierCommsType = electric.SupplierCommsType,
                    ElectricPreSalesStatus = electric.PreSalesStatus,

                    // Gas
                    MPRN = gas.MPRN,
                    GasInputDate = gas.InputDate,
                    GasCurrentSupplier = gas.CurrentSupplier,
                    GasSupplierId = gas.SupplierId,
                    GasProductId = gas.ProductId,
                    GasInitialStartDate = gas.InitialStartDate,
                    GasDuration = gas.Duration,
                    GasSalesType = gas.SalesType,
                    GasSalesTypeStatus = gas.SalesTypeStatus,
                    GasUplift = Convert.ToDecimal(gas.Uplift),
                    GasInputEAC = Convert.ToDecimal(gas.InputEAC),
                    GasUnitRate = Convert.ToDecimal(gas.UnitRate),
                    GasOtherRate = Convert.ToDecimal(gas.OtherRate),
                    GasStandingCharge = Convert.ToDecimal(gas.StandingCharge),
                    GasSupplierCommsType = gas.SupplierCommsType,
                    GasPreSalesStatus = gas.PreSalesStatus
                };

                return View("EditDual", model);
            }
            catch (Exception ex)
            {
                Logger.Log("EditDual failed to load: " + ex.Message);
                return JsonResponse.Fail("Failed to load dual contract.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditDual(DualContractEditViewModel model)
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

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var electric = await _db.CE_ElectricContracts.FirstOrDefaultAsync(e => e.EId == model.EId && e.Type == "Dual");
                    var gas = await _db.CE_GasContracts.FirstOrDefaultAsync(g => g.EId == model.EId && g.Type == "Dual");
                    var account = await _db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Dual");

                    if (electric == null || gas == null)
                        return JsonResponse.Fail("Dual contract not found.");

                    var username = model.EMProcessor ?? "PreSales Team";
                    var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    void Log(object logCollection, string status, string msg)
                    {
                        if (logCollection is DbSet<CE_ElectricContractLogs> elogs)
                            elogs.Add(new CE_ElectricContractLogs { EId = model.EId, Username = username, ActionDate = now, PreSalesStatusType = status, Message = msg });
                        if (logCollection is DbSet<CE_GasContractLogs> glogs)
                            glogs.Add(new CE_GasContractLogs { EId = model.EId, Username = username, ActionDate = now, PreSalesStatusType = status, Message = msg });
                    }

                    // ---- ELECTRIC
                    electric.Department = model.Department;
                    electric.Agent = model.Agent;
                    electric.Source = model.Source;
                    electric.Introducer = model.Introducer;
                    electric.SubIntroducer = model.SubIntroducer;
                    electric.TopLine = model.TopLine;
                    electric.MPAN = model.MPAN;
                    electric.CurrentSupplier = model.ElectricCurrentSupplier;
                    electric.SupplierId = model.ElectricSupplierId;
                    electric.ProductId = model.ElectricProductId;
                    electric.SupplierCommsType = model.ElectricSupplierCommsType;
                    electric.SalesType = model.ElectricSalesType;
                    electric.SalesTypeStatus = model.ElectricSalesTypeStatus;
                    electric.InitialStartDate = model.ElectricInitialStartDate;
                    electric.Duration = model.ElectricDuration;
                    electric.Uplift = model.ElectricUplift.ToString();
                    electric.InputEAC = model.ElectricInputEAC.ToString();
                    electric.DayRate = model.ElectricDayRate.ToString();
                    electric.NightRate = model.ElectricNightRate.ToString();
                    electric.EveWeekendRate = model.ElectricEveWeekendRate.ToString();
                    electric.OtherRate = model.ElectricOtherRate.ToString();
                    electric.StandingCharge = model.ElectricStandingCharge.ToString();
                    electric.BusinessName = model.BusinessName;
                    electric.CustomerName = model.CustomerName;
                    electric.BusinessDoorNumber = model.BusinessDoorNumber;
                    electric.BusinessHouseName = model.BusinessHouseName;
                    electric.BusinessStreet = model.BusinessStreet;
                    electric.BusinessTown = model.BusinessTown;
                    electric.BusinessCounty = model.BusinessCounty;
                    electric.PostCode = model.PostCode;
                    electric.PhoneNumber1 = model.PhoneNumber1;
                    electric.PhoneNumber2 = model.PhoneNumber2;
                    electric.EmailAddress = model.EmailAddress;
                    electric.PreSalesStatus = model.ElectricPreSalesStatus;
                    electric.EMProcessor = model.EMProcessor;
                    electric.ContractChecked = model.ContractChecked;
                    electric.ContractAudited = model.ContractAudited;
                    electric.Terminated = model.Terminated;
                    electric.ContractNotes = model.ContractNotes;
                    electric.UpdatedAt = now;

                    // InputDate trigger
                    var triggeringStatuses = new[] { "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted" };
                    if (triggeringStatuses.Contains(model.ElectricPreSalesStatus))
                        electric.InputDate = DateTime.Now.ToString("dd/MM/yyyy");

                    // ---- GAS
                    gas.Department = model.Department;
                    gas.Agent = model.Agent;
                    gas.Source = model.Source;
                    gas.Introducer = model.Introducer;
                    gas.SubIntroducer = model.SubIntroducer;
                    gas.MPRN = model.MPRN;
                    gas.CurrentSupplier = model.GasCurrentSupplier;
                    gas.SupplierId = model.GasSupplierId;
                    gas.ProductId = model.GasProductId;
                    gas.SupplierCommsType = model.GasSupplierCommsType;
                    gas.SalesType = model.GasSalesType;
                    gas.SalesTypeStatus = model.GasSalesTypeStatus;
                    gas.InitialStartDate = model.GasInitialStartDate;
                    gas.Duration = model.GasDuration;
                    gas.Uplift = model.GasUplift.ToString();
                    gas.InputEAC = model.GasInputEAC.ToString();
                    gas.UnitRate = model.GasUnitRate.ToString();
                    gas.OtherRate = model.GasOtherRate.ToString();
                    gas.StandingCharge = model.GasStandingCharge.ToString();
                    gas.BusinessName = model.BusinessName;
                    gas.CustomerName = model.CustomerName;
                    gas.BusinessDoorNumber = model.BusinessDoorNumber;
                    gas.BusinessHouseName = model.BusinessHouseName;
                    gas.BusinessStreet = model.BusinessStreet;
                    gas.BusinessTown = model.BusinessTown;
                    gas.BusinessCounty = model.BusinessCounty;
                    gas.PostCode = model.PostCode;
                    gas.PhoneNumber1 = model.PhoneNumber1;
                    gas.PhoneNumber2 = model.PhoneNumber2;
                    gas.EmailAddress = model.EmailAddress;
                    gas.PreSalesStatus = model.GasPreSalesStatus;
                    gas.EMProcessor = model.EMProcessor;
                    gas.ContractChecked = model.ContractChecked;
                    gas.ContractAudited = model.ContractAudited;
                    gas.Terminated = model.Terminated;
                    gas.ContractNotes = model.ContractNotes;
                    gas.UpdatedAt = now;

                    if (triggeringStatuses.Contains(model.GasPreSalesStatus))
                        gas.InputDate = DateTime.Now.ToString("dd/MM/yyyy");

                    model.ElectricInputDate = electric.InputDate;
                    model.GasInputDate = gas.InputDate;

                    // Account
                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    // Logs
                    Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Dual Electric contract updated.");
                    Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Dual Gas contract updated.");

                    if (model.ContractChecked)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been checked.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been checked.");
                    }

                    if (model.ContractAudited)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been audited.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been audited.");
                    }

                    if (model.Terminated)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been marked as terminated.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been marked as terminated.");
                    }

                    await _db.SaveChangesAsync();
                    tx.Commit();

                    return JsonResponse.Ok(new
                    {
                        EId = model.EId,
                        Department = electric.Department,
                        Agent = electric.Agent,
                        Source = electric.Source,
                        Introducer = electric.Introducer,
                        SubIntroducer = electric.SubIntroducer,
                        BusinessName = electric.BusinessName,
                        CustomerName = electric.CustomerName,
                        BusinessDoorNumber = electric.BusinessDoorNumber,
                        BusinessHouseName = electric.BusinessHouseName,
                        BusinessStreet = electric.BusinessStreet,
                        BusinessTown = electric.BusinessTown,
                        BusinessCounty = electric.BusinessCounty,
                        PostCode = electric.PostCode,
                        PhoneNumber1 = electric.PhoneNumber1,
                        PhoneNumber2 = electric.PhoneNumber2,
                        EmailAddress = electric.EmailAddress,
                        ElectricInputDate = electric.InputDate,
                        GasInputDate = gas.InputDate,
                        SortCode = account.SortCode,
                        AccountNumber = account.AccountNumber,
                        MPAN = electric.MPAN,
                        MPRN = gas.MPRN,
                        ElectricCurrentSupplier = electric.CurrentSupplier,
                        ElectricSalesType = electric.SalesType,
                        ElectricSalesTypeStatus = electric.SalesTypeStatus,
                        ElectricDuration = electric.Duration,
                        ElectricUplift = electric.Uplift,
                        ElectricInputEAC = electric.InputEAC,
                        ElectricStandingCharge = electric.StandingCharge,
                        ElectricDayRate = electric.DayRate,
                        ElectricNightRate = electric.NightRate,
                        ElectricEveWeekendRate = electric.EveWeekendRate,
                        ElectricOtherRate = electric.OtherRate,
                        ElectricSupplierId = electric.SupplierId,
                        ElectricProductId = electric.ProductId,
                        ElectricSupplierCommsType = electric.SupplierCommsType,
                        ElectricPreSalesStatus = electric.PreSalesStatus,
                        GasCurrentSupplier = gas.CurrentSupplier,
                        GasSalesType = gas.SalesType,
                        GasSalesTypeStatus = gas.SalesTypeStatus,
                        GasDuration = gas.Duration,
                        GasUplift = gas.Uplift,
                        GasStandingCharge = gas.StandingCharge,
                        GasUnitRate = gas.UnitRate,
                        GasSupplierId = gas.SupplierId,
                        GasProductId = gas.ProductId,
                        GasSupplierCommsType = gas.SupplierCommsType,
                        GasPreSalesStatus = gas.PreSalesStatus,
                        electric.ContractChecked,
                        electric.ContractAudited,
                        electric.Terminated,
                        TopLine = electric.TopLine,
                        ElectricInitialStartDate = electric.InitialStartDate,
                        GasInitialStartDate = gas.InitialStartDate,
                        GasInputEAC = gas.InputEAC,
                        GasOtherRate = gas.OtherRate,
                        ContractNotes = electric.ContractNotes,
                        EMProcessor = electric.EMProcessor

                    }, "Dual contract updated successfully!");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Logger.Log("EditDual failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while updating dual contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForDualContract(string eid)
        {
            try
            {
                var electricLogs = await _db.CE_ElectricContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new LogEntry
                    {
                        EId = l.EId,
                        Username = l.Username,
                        ActionDate = l.ActionDate,
                        PreSalesStatusType = l.PreSalesStatusType,
                        Message = l.Message
                    })
                    .ToListAsync();

                var gasLogs = await _db.CE_GasContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new LogEntry
                    {
                        EId = l.EId,
                        Username = l.Username,
                        ActionDate = l.ActionDate,
                        PreSalesStatusType = l.PreSalesStatusType,
                        Message = l.Message
                    })
                    .ToListAsync();

                var seen = new HashSet<string>();
                var finalLogs = new List<LogEntry>();

                foreach (var log in electricLogs.Concat(gasLogs))
                {
                    var key = $"{log.Username?.Trim()}|{log.ActionDate?.Trim()}|{log.PreSalesStatusType?.Trim()}|{log.Message?.Trim()}";
                    //Debug.WriteLine(key);
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        finalLogs.Add(new LogEntry
                        {
                            EId = log.EId,
                            Username = log.Username?.Trim(),
                            ActionDate = log.ActionDate?.Trim(),
                            PreSalesStatusType = log.PreSalesStatusType?.Trim(),
                            Message = log.Message?.Trim()
                        });
                    }
                }

                var sorted = finalLogs.OrderByDescending(l => DateTime.TryParse(l.ActionDate, out var dt) ? dt : DateTime.MinValue).ToList();
                return JsonResponse.Ok(sorted);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForDualContract failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve logs.");
            }
        }

        #endregion
    }
}