using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.BGBContracts
{
    public class BGBContractController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public BGBContractController()
        {
            _db = new ApplicationDBContext();
        }

        [HttpGet]
        public async Task<ActionResult> EditBGBContract(string id, string supplierId, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _) ||
                    string.IsNullOrWhiteSpace(supplierId) || !int.TryParse(supplierId, out _) ||
                    string.IsNullOrWhiteSpace(type))
                {
                    return HttpNotFound("Invalid ID, SupplierId, or Type.");
                }

                var model = new EditBGBContractViewModel
                {
                    Id = id,
                    SupplierId = supplierId
                };

                if (Regex.IsMatch(type, @"^\d{13}$")) // ---- Electric Section ----
                {
                    var electricContract = await _db.CE_ElectricContracts
                        .FirstOrDefaultAsync(e => e.EId == id && e.MPAN == type);

                    if (electricContract == null)
                        return HttpNotFound("Electric contract not found.");

                    model.Department = electricContract.Department;
                    model.BusinessName = electricContract.BusinessName;
                    model.SalesTypeElectric = electricContract.SalesType;
                    model.MPAN = electricContract.MPAN;
                    model.DurationElectric = electricContract.Duration;
                    model.UpliftElectric = electricContract.Uplift?.ToString();
                    model.ContractNotes = electricContract.ContractNotes;
                    model.HasElectricDetails = true;

                    if (!string.IsNullOrWhiteSpace(electricContract.InputDate) &&
                        DateTime.TryParse(electricContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateElectric = parsedInputDate.ToString("yyyy-MM-dd");
                    }

                    var snapshot = await _db.CE_ElectricSupplierSnapshots
                        .FirstOrDefaultAsync(s => s.EId == id && s.SupplierId == electricContract.SupplierId);

                    if (snapshot != null)
                    {
                        long snapshotId = snapshot.Id;

                        var productSnapshots = await _db.CE_ElectricSupplierProductSnapshots
                            .Where(p => p.SnapshotId == snapshotId && p.SupplierId == snapshot.SupplierId)
                            .ToListAsync();

                        model.ProductElectricList = productSnapshots
                            .Select(p => new SelectListItem
                            {
                                Value = p.ProductId.ToString(),
                                Text = p.ProductName
                            })
                            .ToList();

                        var matchedProduct = productSnapshots
                            .FirstOrDefault(p => p.ProductId == electricContract.ProductId);
                        if (matchedProduct != null)
                        {
                            model.SelectedProductElectric = matchedProduct.ProductId;
                            model.SupplierCommsTypeElectric = matchedProduct.SupplierCommsType;
                            model.CommissionElectric = matchedProduct.Commission?.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(electricContract.InitialStartDate) &&
                        DateTime.TryParse(electricContract.InitialStartDate, out DateTime parsedStartDate))
                    {
                        model.InitialStartDate = parsedStartDate.ToString("yyyy-MM-dd");

                        if (!string.IsNullOrWhiteSpace(electricContract.Duration) &&
                            int.TryParse(electricContract.Duration, out int durationYears))
                        {
                            var cedDate = parsedStartDate.AddYears(durationYears).AddDays(-1);
                            model.CED = cedDate.ToString("yyyy-MM-dd");
                        }
                    }
                }
                else if (Regex.IsMatch(type, @"^\d{6,10}$")) // ---- Gas Section ----
                {
                    var gasContract = await _db.CE_GasContracts
                        .FirstOrDefaultAsync(g => g.EId == id && g.MPRN == type);

                    if (gasContract == null)
                        return HttpNotFound("Gas contract not found.");

                    model.Department = gasContract.Department;
                    model.BusinessName = gasContract.BusinessName;
                    model.SalesTypeGas = gasContract.SalesType;
                    model.MPRN = gasContract.MPRN;
                    model.DurationGas = gasContract.Duration;
                    model.UpliftGas = gasContract.Uplift?.ToString();
                    model.ContractNotes = gasContract.ContractNotes;
                    model.HasGasDetails = true;

                    if (!string.IsNullOrWhiteSpace(gasContract.InputDate) &&
                        DateTime.TryParse(gasContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateGas = parsedInputDate.ToString("yyyy-MM-dd");
                    }

                    var snapshot = await _db.CE_GasSupplierSnapshots
                        .FirstOrDefaultAsync(s => s.EId == id && s.SupplierId == gasContract.SupplierId);

                    if (snapshot != null)
                    {
                        long snapshotId = snapshot.Id;

                        var productSnapshots = await _db.CE_GasSupplierProductSnapshots
                            .Where(p => p.SnapshotId == snapshotId && p.SupplierId == snapshot.SupplierId)
                            .ToListAsync();

                        model.ProductGasList = productSnapshots
                            .Select(p => new SelectListItem
                            {
                                Value = p.ProductId.ToString(),
                                Text = p.ProductName
                            })
                            .ToList();

                        var matchedProduct = productSnapshots
                            .FirstOrDefault(p => p.ProductId == gasContract.ProductId);
                        if (matchedProduct != null)
                        {
                            model.SelectedProductGas = matchedProduct.ProductId;
                            model.SupplierCommsTypeGas = matchedProduct.SupplierCommsType;
                            model.CommissionGas = matchedProduct.Commission?.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(gasContract.InitialStartDate) &&
                        DateTime.TryParse(gasContract.InitialStartDate, out DateTime parsedStartDate))
                    {
                        model.InitialStartDate = parsedStartDate.ToString("yyyy-MM-dd");

                        if (!string.IsNullOrWhiteSpace(gasContract.Duration) &&
                            int.TryParse(gasContract.Duration, out int durationYears))
                        {
                            var cedDate = parsedStartDate.AddYears(durationYears).AddDays(-1);
                            model.CED = cedDate.ToString("yyyy-MM-dd");
                        }
                    }
                }
                else
                {
                    return HttpNotFound("Unknown contract type.");
                }

                if (int.TryParse(supplierId, out int supId))
                {
                    var latestUpload = await _db.CE_InvoiceSupplierUploads
                        .Where(x => x.SupplierId == supId)
                        .OrderByDescending(x => x.UploadedOn)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new { x.FileName, x.UploadedOn })
                        .FirstOrDefaultAsync();

                    if (latestUpload != null && !string.IsNullOrWhiteSpace(latestUpload.FileName))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(latestUpload.FileName);
                        var parts = fileName.Split('_');

                        if (parts.Length == 2 &&
                            !string.IsNullOrWhiteSpace(parts[0]) &&
                            DateTime.TryParseExact(parts[1], "dd.MM.yyyy",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate))
                        {
                            model.InvoiceNo = parts[0];
                            model.InvoiceDate = invoiceDate.ToString("yyyy-MM-dd");
                        }
                    }
                }

                model.PaymentDate = CalculatePaymentDate(model.InvoiceDate);

                return View("~/Views/Accounts/BGBContract/EditBGBContract.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log($"EditBGBContract failed for id={id}, supplierId={supplierId}, type={type}: {ex}");
                return RedirectToAction("NotFound", "Error");
            }
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateContract(UpdateContractViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.EId))
                return JsonResponse.Fail("Invalid data submitted.");

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    var electricContract = await _db.CE_ElectricContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);

                    var gasContract = await _db.CE_GasContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);

                    // ----- Electric -----
                    if (electricContract != null)
                    {
                        if (model.HasElectricDetails)
                        {
                            electricContract.Uplift = model.UpliftElectric;
                            electricContract.SupplierCommsType = model.SupplierCommsTypeElectric;

                            var electricSnapshot = await _db.CE_ElectricSupplierSnapshots
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.SupplierId == electricContract.SupplierId);

                            if (electricSnapshot != null)
                            {
                                var productSnapshot = await _db.CE_ElectricSupplierProductSnapshots
                                    .FirstOrDefaultAsync(p => p.SnapshotId == electricSnapshot.Id
                                                           && p.ProductId == electricContract.ProductId);
                                if (productSnapshot != null)
                                {
                                    productSnapshot.SupplierCommsType = model.SupplierCommsTypeElectric;
                                }
                            }
                        }

                        electricContract.ContractNotes = model.ContractNotes;
                    }

                    // ----- Gas -----
                    if (gasContract != null)
                    {
                        if (model.HasGasDetails)
                        {
                            gasContract.Uplift = model.UpliftGas;
                            gasContract.SupplierCommsType = model.SupplierCommsTypeGas;

                            var gasSnapshot = await _db.CE_GasSupplierSnapshots
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.SupplierId == gasContract.SupplierId);

                            if (gasSnapshot != null)
                            {
                                var productSnapshot = await _db.CE_GasSupplierProductSnapshots
                                    .FirstOrDefaultAsync(p => p.SnapshotId == gasSnapshot.Id
                                                           && p.ProductId == gasContract.ProductId);
                                if (productSnapshot != null)
                                {
                                    productSnapshot.SupplierCommsType = model.SupplierCommsTypeGas;
                                }
                            }
                        }

                        gasContract.ContractNotes = model.ContractNotes;
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return JsonResponse.Ok(null, "Contract updated successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("UpdateContract failed: " + ex);
                    return JsonResponse.Fail("An error occurred while updating the contract.");
                }
            }
        }


        #region Invoice_Logs

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> SaveEacLog(BGBEacLogViewModel model)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!ModelState.IsValid)
                        return Json(JsonResponse.Fail("Please fill all required fields correctly."));

                    var log = new CE_BGBEacLogs
                    {
                        EId = model.EId,
                        EacYear = model.EacYear?.Trim(),
                        EacValue = model.EacValue?.Trim(),
                        FinalEac = model.FinalEac?.Trim(),
                        InvoiceNo = model.InvoiceNo?.Trim(),
                        InvoiceDate = model.InvoiceDate?.Trim(),
                        PaymentDate = model.PaymentDate?.Trim(),
                        InvoiceAmount = model.InvoiceAmount?.Trim(),
                        SupplierEacD19 = model.SupplierEacD19.ToString(),
                        CreatedAt = DateTime.Now
                    };

                    _db.CE_BGBEacLogs.Add(log);
                    await _db.SaveChangesAsync();

                    var logs = await _db.CE_BGBEacLogs
                        .Where(x => x.EId == model.EId)
                        .OrderByDescending(x => x.CreatedAt)
                        .Select(x => new
                        {
                            x.Id,
                            x.EacYear,
                            x.EacValue,
                            x.FinalEac,
                            x.InvoiceNo,
                            x.InvoiceDate,
                            x.PaymentDate,
                            x.InvoiceAmount,
                            x.SupplierEacD19,
                            x.CreatedAt
                        })
                        .ToListAsync();

                    var formattedLogs = logs.Select(x => new
                    {
                        x.Id,
                        x.EacYear,
                        x.EacValue,
                        x.FinalEac,
                        x.InvoiceNo,
                        InvoiceDate = DateTime.TryParse(x.InvoiceDate, out var dt) ? dt.ToString("dd-MM-yyyy") : x.InvoiceDate,
                        PaymentDate = DateTime.TryParse(x.PaymentDate, out var dtp) ? dtp.ToString("dd-MM-yyyy") : x.PaymentDate,
                        x.InvoiceAmount,
                        x.SupplierEacD19,
                        MPAN = _db.CE_ElectricContracts
                        .Where(ec => ec.EId == model.EId)
                        .Select(ec => ec.MPAN)
                        .FirstOrDefault(),
                        MPRN = _db.CE_GasContracts
                        .Where(gc => gc.EId == model.EId)
                        .Select(gc => gc.MPRN)
                        .FirstOrDefault(),
                        Timestamp = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();

                    transaction.Commit();
                    return JsonResponse.Ok(formattedLogs, "EAC Log saved successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("SaveEacLog failed: " + ex);
                    return JsonResponse.Fail("Something went wrong while saving the EAC Log.");
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEacLogs(string eid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Invalid EId.");

                var logs = await _db.CE_BGBEacLogs
                    .Where(x => x.EId == eid)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                var electricContract = await _db.CE_ElectricContracts
                    .Where(c => c.EId == eid)
                    .Select(c => c.MPAN)
                    .FirstOrDefaultAsync();

                var gasContract = await _db.CE_GasContracts
                    .Where(c => c.EId == eid)
                    .Select(c => c.MPRN)
                    .FirstOrDefaultAsync();

                var formattedLogs = logs.Select(x => new
                {
                    x.Id,
                    x.EacYear,
                    x.EacValue,
                    x.FinalEac,
                    x.InvoiceNo,
                    InvoiceDate = DateTime.TryParse(x.InvoiceDate, out var dt) ? dt.ToString("dd-MM-yyyy") : x.InvoiceDate,
                    PaymentDate = DateTime.TryParse(x.PaymentDate, out var dtp) ? dtp.ToString("dd-MM-yyyy") : x.PaymentDate,
                    x.InvoiceAmount,
                    x.SupplierEacD19,
                    MPAN = electricContract,
                    MPRN = gasContract,
                    Timestamp = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                return JsonResponse.Ok(formattedLogs);
            }
            catch (Exception ex)
            {
                Logger.Log($"GetEacLogs failed for EId={eid}: " + ex);
                return JsonResponse.Fail("Unable to fetch EAC logs.");
            }
        }

        #endregion

        #region Helper Methods
        private string CalculatePaymentDate(string invoiceDateStr)
        {
            if (DateTime.TryParseExact(invoiceDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate))
            {
                var fourthWeekStart = invoiceDate.AddDays(21 + 7);

                var diffToFriday = DayOfWeek.Friday - fourthWeekStart.DayOfWeek;
                if (diffToFriday < 0) diffToFriday += 7;
                var paymentDate = fourthWeekStart.AddDays(diffToFriday);

                return paymentDate.ToString("yyyy-MM-dd");
            }
            return null;
        }

        #endregion
    }
}