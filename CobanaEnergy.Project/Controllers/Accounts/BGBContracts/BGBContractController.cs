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
        public ActionResult EditBGBContract(string id, string supplierId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _) ||
                    string.IsNullOrWhiteSpace(supplierId) || !int.TryParse(supplierId, out _))
                    return HttpNotFound("Invalid ID or SupplierId.");

                var model = new EditBGBContractViewModel
                {
                    Id = id,
                    SupplierId = supplierId
                };

                // Fetch Electric Contract
                var electricContract = _db.CE_ElectricContracts.FirstOrDefault(e => e.EId == id);
                if (electricContract != null)
                {
                    model.Department = electricContract.Department;
                    model.BusinessName = electricContract.BusinessName;
                    model.SalesTypeElectric = electricContract.SalesType;
                    model.MPAN = electricContract.MPAN;
                    model.DurationElectric = electricContract.Duration;
                    if (!string.IsNullOrWhiteSpace(electricContract.InputDate) &&
                         DateTime.TryParse(electricContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateElectric = parsedInputDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        model.InputDateElectric = null;
                    }
                    model.ContractNotes = electricContract.ContractNotes;
                }
                else
                {
                    model.HasElectricDetails = false;
                }

                // Fetch Gas Contract
                var gasContract = _db.CE_GasContracts.FirstOrDefault(g => g.EId == id);
                if (gasContract != null)
                {
                    model.SalesTypeGas = gasContract.SalesType;
                    model.MPRN = gasContract.MPRN;
                    model.DurationGas = gasContract.Duration;
                    if (!string.IsNullOrWhiteSpace(gasContract.InputDate) &&
                        DateTime.TryParse(gasContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateGas = parsedInputDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        model.InputDateGas = null;
                    }
                }
                else
                {
                    model.HasGasDetails = false;
                }

                if (int.TryParse(supplierId, out int supId))
                {
                    var latestUpload = _db.CE_InvoiceSupplierUploads
                        .Where(x => x.SupplierId == supId)
                        .OrderByDescending(x => x.UploadedOn)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new { x.FileName, x.UploadedOn })
                        .FirstOrDefault();

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
                Logger.Log("EditBGBContract failed for id=" + id + ", supplierId=" + supplierId + ": " + ex);
                return RedirectToAction("NotFound", "Error");
            }
        }

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

                // Get all logs for this EId
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
    }
}