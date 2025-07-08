using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        [HttpGet]
        [Authorize(Roles = "Accounts,Admin")]
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
        public async Task<JsonResult> UploadInvoiceFile(InvoiceSupplierUploadViewModel model, HttpPostedFileBase InvoiceFile)
        {
            if (model.SupplierId <= 0 || !await db.CE_Supplier.AnyAsync(s => s.Id == model.SupplierId && s.Status))
                return JsonResponse.Fail("Invalid supplier selected.");

            if (InvoiceFile == null || InvoiceFile.ContentLength == 0)
                return JsonResponse.Fail("Please upload a valid file.");

            if (InvoiceFile.ContentLength > (10 * 1024 * 1024))
                return JsonResponse.Fail("File size exceeds 10 MB limit.");

            var extension = Path.GetExtension(InvoiceFile.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                return JsonResponse.Fail("Only .xlsx and .csv files are allowed.");

            try
            {
                using (var binaryReader = new BinaryReader(InvoiceFile.InputStream))
                {
                    var fileBytes = binaryReader.ReadBytes(InvoiceFile.ContentLength);

                    var upload = new CE_InvoiceSupplierUploads
                    {
                        SupplierId = model.SupplierId,
                        FileName = Path.GetFileName(InvoiceFile.FileName),
                        FileContent = fileBytes,
                        UploadedBy = User.Identity.Name ?? "Unknown",
                        //UploadedOn = DateTime.UtcNow
                    };

                    db.CE_InvoiceSupplierUploads.Add(upload);
                    await db.SaveChangesAsync();

                    return JsonResponse.Ok(new { uploadId = upload.Id }, "File uploaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("UploadInvoiceFile: " + ex);
                return JsonResponse.Fail("An error occurred while uploading the file.");
            }
        }

    }
}