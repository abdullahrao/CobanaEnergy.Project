using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using CsvHelper;
using Logic;
using Logic.ResponseModel.Helper;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.InvoiceSupplierDashboard
{
    public class InvoiceSupplierDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public InvoiceSupplierDashboardController()
        {
            db = new ApplicationDBContext();
        }

        #region popup

        [HttpGet]
        [Authorize(Roles = "Accounts,Admin,Controls")]
        public async Task<PartialViewResult> InvoiceSupplierPopup()
        {
            try
            {
                var activeSuppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var model = new InvoiceSupplierUploadViewModel
                {
                    Suppliers = new SelectList(activeSuppliers, "Id", "Name")
                };

                return PartialView("~/Views/Accounts/InvoiceSupplierDashboard/InvoiceSupplierPopup.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("InvoiceSupplierPopup: " + ex);
                return PartialView("~/Views/Shared/_ModalError.cshtml", "Failed to load popup.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts,Admin,Controls")]
        public async Task<JsonResult> UploadInvoiceFile(InvoiceSupplierUploadViewModel model, HttpPostedFileBase InvoiceFile)
        {
            if (model.SupplierId <= 0 || !await db.CE_Supplier.AnyAsync(s => s.Id == model.SupplierId && s.Status))
                return JsonResponse.Fail("Invalid supplier selected.");

            if (InvoiceFile == null || InvoiceFile.ContentLength == 0)
                return JsonResponse.Fail("Please upload a valid file.");

            if (InvoiceFile.ContentLength > (10 * 1024 * 1024))
                return JsonResponse.Fail("File size exceeds 10 MB limit.");

            var extension = Path.GetExtension(InvoiceFile.FileName)?.ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return JsonResponse.Fail("Only .xlsx, .xls, and .csv files are allowed.");

            try
            {
                var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == model.SupplierId);
                if (supplier == null)
                    return JsonResponse.Fail("Supplier not found.");

                if (string.IsNullOrWhiteSpace(supplier.Name))
                    return JsonResponse.Fail("Invalid supplier. Please contact support.");

                byte[] fileBytes;
                using (var binaryReader = new BinaryReader(InvoiceFile.InputStream))
                {
                    fileBytes = binaryReader.ReadBytes(InvoiceFile.ContentLength);
                }

                List<string> meterNumbers = null;
                using (var memStream = new MemoryStream(fileBytes))
                {
                    if (supplier.Name?.Trim().ToLowerInvariant() == "british gas business")
                    {
                        meterNumbers = await BGBSupplier(memStream, extension);
                    }
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }
                }

                if (meterNumbers == null || meterNumbers.Count == 0)
                    return JsonResponse.Fail("Could not find MeterNum or MeterPoint column in the uploaded file. Please check your file and try again.");

                var upload = new CE_InvoiceSupplierUploads
                {
                    SupplierId = model.SupplierId,
                    FileName = Path.GetFileName(InvoiceFile.FileName),
                    FileContent = fileBytes,
                    UploadedBy = User.Identity.Name ?? "Unknown",
                };

                db.CE_InvoiceSupplierUploads.Add(upload);
                await db.SaveChangesAsync();

                return JsonResponse.Ok(
                       new
                       {
                           redirectUrl = Url.Action("ContractSelectListing", "InvoiceSupplierDashboard", new { uploadId = upload.Id })
                       },
                           "File uploaded and processed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UploadInvoiceFile: " + ex);
                return JsonResponse.Fail("An error occurred while uploading the file.");
            }
        }

        #endregion

        #region BGBSupplier

        static readonly string[] BGBMeterHeaders = { "meternum", "meterpoint" };
        public async Task<List<string>> BGBSupplier(Stream fileStream, string extension)
        {
            try
            {
                var uniqueMeterNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                fileStream.Position = 0;
                int maxScanRows = 120;

                if (extension == ".xlsx" || extension == ".xls")
                {
                    ISheet sheet;
                    if (extension == ".xlsx")
                    {
                        var workbook = new XSSFWorkbook(fileStream);
                        sheet = GetBackingDataSheet(workbook);
                    }
                    else
                    {
                        var workbook = new HSSFWorkbook(fileStream);
                        sheet = GetBackingDataSheet(workbook);
                    }

                    int headerRowIdx = -1;
                    int meterColIdx = -1;

                    for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum && rowIdx < maxScanRows; rowIdx++)
                    {
                        var row = sheet.GetRow(rowIdx);
                        if (row == null) continue;

                        for (int cellIdx = 0; cellIdx < row.LastCellNum; cellIdx++)
                        {
                            var cellVal = row.GetCell(cellIdx)?.ToString();
                            if (!string.IsNullOrWhiteSpace(cellVal))
                            {
                                string normalized = cellVal
                                    .Replace("\u00A0", " ")
                                    .Replace("\u200B", "")
                                    .Replace(" ", "")
                                    .Trim()
                                    .ToLowerInvariant();

                                //System.Diagnostics.Debug.WriteLine($"Row {rowIdx} Cell {cellIdx}: '{normalized}'");

                                if (BGBMeterHeaders.Contains(normalized))
                                {
                                    headerRowIdx = rowIdx;
                                    meterColIdx = cellIdx;
                                    break;
                                }
                            }
                        }
                        if (meterColIdx != -1) break;
                    }

                    if (headerRowIdx == -1 || meterColIdx == -1)
                        return uniqueMeterNumbers.ToList();

                    for (int rowIdx = headerRowIdx + 1; rowIdx <= sheet.LastRowNum; rowIdx++)
                    {
                        var row = sheet.GetRow(rowIdx);
                        if (row == null) continue;
                        var val = row.GetCell(meterColIdx)?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(val))
                            uniqueMeterNumbers.Add(val);
                    }
                }
                else if (extension == ".csv")
                {
                    fileStream.Position = 0;
                    using (var reader = new StreamReader(fileStream))
                    using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    }))
                    {
                        int meterColIdx = -1;
                        string[] header = null;
                        int rowNum = 0;
                        while (csv.Read())
                        {
                            rowNum++;
                            if (!csv.Context.Parser.Record.All(f => string.IsNullOrWhiteSpace(f)))
                            {
                                header = csv.Context.Parser.Record;
                                for (int i = 0; i < header.Length; i++)
                                {
                                    var h = header[i]
                                        ?.Replace("\u00A0", " ")
                                        .Replace("\u200B", "")
                                        .Replace(" ", "")
                                        .Trim()
                                        .ToLowerInvariant();
                                    if (BGBMeterHeaders.Contains(h))
                                    {
                                        meterColIdx = i;
                                        break;
                                    }
                                }
                                break;
                            }
                            if (rowNum > maxScanRows) break;
                        }
                        if (meterColIdx == -1)
                            return uniqueMeterNumbers.ToList();

                        while (csv.Read())
                        {
                            var val = csv.GetField(meterColIdx)?.Trim();
                            if (!string.IsNullOrWhiteSpace(val))
                                uniqueMeterNumbers.Add(val);
                        }
                    }
                }
                return uniqueMeterNumbers.ToList();
            }
            catch (Exception ex)
            {
                Logger.Log("BGBSupplier: " + ex);
                return new List<string>();
            }

        }


        #endregion

        #region Helper Methods
        private ISheet GetBackingDataSheet(IWorkbook workbook)
        {
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var name = workbook.GetSheetName(i)?.Replace(" ", "").Trim().ToLowerInvariant();
                if (name == "backingdata")
                    return workbook.GetSheetAt(i);
            }
            return workbook.GetSheetAt(0);
        }

        #endregion

        #region contract slect listing

        [HttpGet]
        [Authorize(Roles = "Accounts,Admin,Controls")]
        public async Task<ActionResult> ContractSelectListing(int uploadId)
        {
            try
            {
                var upload = await db.CE_InvoiceSupplierUploads.FirstOrDefaultAsync(x => x.Id == uploadId);
                if (upload == null)
                    return JsonResponse.Fail("Could not find uploaded invoice.");

                var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == upload.SupplierId);
                if (supplier == null)
                    return JsonResponse.Fail("Supplier not found for this invoice.");

                var extension = Path.GetExtension(upload.FileName)?.ToLowerInvariant();

                List<string> meterNumbers;
                using (var memStream = new MemoryStream(upload.FileContent))
                {
                    if (supplier.Name?.Trim().ToLowerInvariant() == "british gas business")
                    {
                        meterNumbers = await BGBSupplier(memStream, extension);
                    }
                    // Add more suppliers here!
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }
                }

                var mpans = meterNumbers.Where(x => x.All(char.IsDigit) && x.Length == 13).ToList();
                var mprns = meterNumbers.Where(x => x.All(char.IsDigit) && x.Length >= 6 && x.Length <= 10).ToList();

                var electricContractsRaw = await (from ec in db.CE_ElectricContracts
                                                  where mpans.Contains(ec.MPAN)
                                                  join s in db.CE_Supplier on ec.SupplierId equals s.Id into s1
                                                  from s in s1.DefaultIfEmpty()
                                                  select new
                                                  {
                                                      EId = ec.EId,
                                                      SupplierName = s != null ? s.Name : "",
                                                      MPAN = ec.MPAN,
                                                      InputDate = ec.InputDate,
                                                      BusinessName = ec.BusinessName,
                                                      InputEAC = ec.InputEAC,
                                                      Duration = ec.Duration
                                                  }).ToListAsync();

                var electricContracts = electricContractsRaw.Select(x => new ContractSelectRowViewModel
                {
                    EId = x.EId,
                    SupplierName = x.SupplierName,
                    MPAN = x.MPAN,
                    MPRN = null,
                    InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                    BusinessName = x.BusinessName,
                    InputEAC = x.InputEAC,
                    Duration = x.Duration,
                    ContractStatus = "",
                    PaymentStatus = "",
                    CED = ""
                }).ToList();

                var gasContractsRaw = await (from gc in db.CE_GasContracts
                                             where mprns.Contains(gc.MPRN)
                                             join s in db.CE_Supplier on gc.SupplierId equals s.Id into s2
                                             from s in s2.DefaultIfEmpty()
                                             select new
                                             {
                                                 EId = gc.EId,
                                                 SupplierName = s != null ? s.Name : "",
                                                 MPRN = gc.MPRN,
                                                 InputDate = gc.InputDate,
                                                 BusinessName = gc.BusinessName,
                                                 InputEAC = gc.InputEAC,
                                                 Duration = gc.Duration
                                             }).ToListAsync();

                var gasContracts = gasContractsRaw.Select(x => new ContractSelectRowViewModel
                {
                    EId = x.EId,
                    SupplierName = x.SupplierName,
                    MPAN = null,
                    MPRN = x.MPRN,
                    InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                    BusinessName = x.BusinessName,
                    InputEAC = x.InputEAC,
                    Duration = x.Duration,
                    ContractStatus = "",
                    PaymentStatus = "",
                    CED = ""
                }).ToList();

                var allContracts = electricContracts.Concat(gasContracts).ToList();

                var model = new ContractSelectListingViewModel
                {
                    UploadId = uploadId,
                    Contracts = allContracts,
                    SupplierName = allContracts.FirstOrDefault()?.SupplierName ?? "NULL"
                };

                return View("~/Views/Accounts/InvoiceSupplierDashboard/ContractSelectListing.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("ContractSelectListing: " + ex);
                return View("~/Views/Shared/_ModalError.cshtml", (object)"An error occurred while loading contract listing.");
            }
        }

        #endregion

        #region Confirm Selection & Edit Contracts

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ConfirmSelectionInvoiceSupplier(List<string> selectedContracts)
        {
            try
            {
                if (selectedContracts == null || !selectedContracts.Any())
                    return JsonResponse.Fail("No contracts selected.");

                TempData["SelectedContractIds"] = selectedContracts;

                return JsonResponse.Ok(new { redirectUrl = Url.Action("EditContractsInvoiceSupplier") });
            }
            catch (Exception ex)
            {
                Logger.Log("Error in ConfirmSelectionInvoiceSupplier: " + ex);
                return JsonResponse.Fail("Something went wrong while confirming selection. Please try again later.");
            }
        }

        [HttpGet]
        public async Task<ActionResult> EditContractsInvoiceSupplier()
        {
            try
            {
                var selectedIds = TempData["SelectedContractIds"] as List<string>;
                if (selectedIds == null || !selectedIds.Any())
                    return RedirectToAction("NotFound", "Error");

                // Fetch contracts
                var electricContracts = await db.CE_ElectricContracts
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();

                var gasContracts = await db.CE_GasContracts
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();

                // Get snapshot entries by EId
                var electricSnapshots = await db.CE_ElectricSupplierSnapshots
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();
                var electricSnapshotDict = electricSnapshots.ToDictionary(x => x.EId, x => x);
                var electricSnapshotIds = electricSnapshots.Select(x => x.Id).ToList();

                var gasSnapshots = await db.CE_GasSupplierSnapshots
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();
                var gasSnapshotDict = gasSnapshots.ToDictionary(x => x.EId, x => x);
                var gasSnapshotIds = gasSnapshots.Select(x => x.Id).ToList();

                // Fetch relevant product & uplift snapshots (only for these snapshot IDs)
                var electricProductSnapshotDict = await db.CE_ElectricSupplierProductSnapshots
                    .Where(x => electricSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var electricUpliftSnapshotDict = await db.CE_ElectricSupplierUpliftSnapshots
                    .Where(x => electricSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var gasProductSnapshotDict = await db.CE_GasSupplierProductSnapshots
                    .Where(x => gasSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var gasUpliftSnapshotDict = await db.CE_GasSupplierUpliftSnapshots
                    .Where(x => gasSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var contracts = new List<ContractEditRowViewModel>();

                // Electric contracts
                foreach (var x in electricContracts)
                {
                    string uplift = "N/A";
                    string supplierCommsType = "N/A";

                    if (electricSnapshotDict.TryGetValue(x.EId, out var snapshot))
                    {
                        var snapshotId = snapshot.Id;

                        if (electricUpliftSnapshotDict.TryGetValue(snapshotId, out var upliftSnap) && upliftSnap != null)
                            uplift = upliftSnap.Uplift ?? "N/A";

                        if (electricProductSnapshotDict.TryGetValue(snapshotId, out var productSnap) && productSnap != null)
                            supplierCommsType = productSnap.SupplierCommsType ?? "N/A";
                    }

                    contracts.Add(new ContractEditRowViewModel
                    {
                        EId = x.EId,
                        MPAN = x.MPAN,
                        MPRN = null,
                        InputDate = x.InputDate,
                        BusinessName = x.BusinessName,
                        StartDate = x.InitialStartDate,
                        Duration = x.Duration,
                        Uplift = uplift,
                        SupplierCommsType = supplierCommsType,
                        CED = "",
                        CED_COT = "",
                        FuelType = "Electric"
                    });
                }

                // Gas contracts
                foreach (var x in gasContracts)
                {
                    string uplift = "N/A";
                    string supplierCommsType = "N/A";

                    if (gasSnapshotDict.TryGetValue(x.EId, out var snapshot))
                    {
                        var snapshotId = snapshot.Id;

                        if (gasUpliftSnapshotDict.TryGetValue(snapshotId, out var upliftSnap) && upliftSnap != null)
                            uplift = upliftSnap.Uplift ?? "N/A";

                        if (gasProductSnapshotDict.TryGetValue(snapshotId, out var productSnap) && productSnap != null)
                            supplierCommsType = productSnap.SupplierCommsType ?? "N/A";
                    }

                    contracts.Add(new ContractEditRowViewModel
                    {
                        EId = x.EId,
                        MPAN = null,
                        MPRN = x.MPRN,
                        InputDate = x.InputDate,
                        BusinessName = x.BusinessName,
                        StartDate = x.InitialStartDate,
                        Duration = x.Duration,
                        Uplift = uplift,
                        SupplierCommsType = supplierCommsType,
                        CED = "",
                        CED_COT = "",
                        FuelType = "Gas"
                    });
                }

                var model = new ContractEditTableViewModel
                {
                    Contracts = contracts
                };

                return View("~/Views/Accounts/InvoiceSupplierDashboard/EditContractsInvoiceSupplier.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("Error in EditContractsInvoiceSupplier" + ex);
                return RedirectToAction("NotFound", "Error");
            }
        }

        #endregion

    }
}