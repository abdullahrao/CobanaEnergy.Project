using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Supplier;
using CobanaEnergy.Project.Models.Supplier.Edit_Supplier;
using CobanaEnergy.Project.Models.Supplier.Supplier_Dashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
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
    public class SupplierController : BaseController
    {
        private readonly ApplicationDBContext db;

        public SupplierController()
        {
            db = new ApplicationDBContext();
        }

        #region supplier_creation

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult SupplierCreation()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> SupplierCreation(SupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                //return JsonResponse.Fail("Please correct the errors in the form.");
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return JsonResponse.Fail(string.Join("<br>", errors));
            }


            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var supplier = new CE_Supplier
                    {
                        Name = model.Name,
                        Status = true,
                        Link = model.Link,
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    db.CE_Supplier.Add(supplier);
                    await db.SaveChangesAsync();

                    foreach (var contact in model.Contacts)
                    {
                        db.CE_SupplierContacts.Add(new CE_SupplierContacts
                        {
                            SupplierId = supplier.Id,
                            ContactName = contact.ContactName,
                            Role = contact.Role,
                            PhoneNumber = contact.PhoneNumber,
                            Email = contact.Email,
                            Notes = contact.Notes
                        });
                    }

                    foreach (var product in model.Products)
                    {
                        db.CE_SupplierProducts.Add(new CE_SupplierProducts
                        {
                            SupplierId = supplier.Id,
                            ProductName = product.ProductName,
                            StartDate = product.StartDate,
                            EndDate = "3099-06-23",// product.EndDate,
                            Commission = product.Commission,
                            SupplierCommsType = product.SupplierCommsType
                        });
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    //Logger.Log($"Supplier created successfully: {supplier.Name} by {User.Identity.Name}");
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log($"Supplier creation failed: {ex.Message}");
                    return JsonResponse.Fail("An unexpected error occurred while saving supplier.");
                }
            }
        }

        #endregion

        #region supplier_dashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult SupplierDashboard()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetActiveSuppliers()
        {
            try
            {
                var today = DateTime.Today;

                var suppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .Include(s => s.CE_SupplierProducts).Where(s => s != null)
                    .ToListAsync();

                var data = suppliers
                    .SelectMany(s => s.CE_SupplierProducts
                        .Where(p =>
                            DateTime.TryParse(p.EndDate, out var endDate) &&
                            endDate > today)
                        .Select(p => new SupplierDashboardItemViewModel
                        {
                            SupplierId = s.Id,
                            SupplierName = s.Name,
                            Link = s.Link,
                            ProductName = p.ProductName,
                            Commission = p.Commission,
                            Status = s.Status
                        }))
                    .ToList();

                return JsonResponse.Ok(data);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load suppliers dashboard: " + ex.Message);
                return JsonResponse.Fail("Failed to load suppliers.");
            }
        }

        #endregion

        #region edit_supplier

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult EditSupplier()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSupplierForEdit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || !int.TryParse(id, out int supplierId))
                    return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Invalid or missing supplier ID.");

                var supplier = await db.CE_Supplier
                    .Include(s => s.CE_SupplierContacts)
                    .Include(s => s.CE_SupplierProducts)
                    .FirstOrDefaultAsync(s => s.Id == supplierId && s.Status);

                if (supplier == null)
                    return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier not found or inactive.");

                var viewModel = new EditSupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Link = supplier.Link,
                    Status = supplier.Status,
                    Contacts = supplier.CE_SupplierContacts.Select(c => new SupplierContactViewModel
                    {
                        Id = c.Id,
                        ContactName = c.ContactName,
                        Role = c.Role,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Notes = c.Notes
                    }).ToList(),
                    Products = supplier.CE_SupplierProducts.Select(p => new SupplierProductViewModel
                    {
                        Id = p.Id,
                        ProductName = p.ProductName,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Commission = p.Commission,
                        SupplierCommsType = p.SupplierCommsType

                    }).ToList()
                };

                return JsonResponse.Ok(viewModel);
            }
            catch (Exception ex)
            {
                Logger.Log("Error loading supplier for edit: " + ex.Message);
                return JsonResponse.Fail("Something went wrong while loading the supplier.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditSupplier(EditSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                //return JsonResponse.Fail("Please correct the errors in the form.");
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var supplier = await db.CE_Supplier
                        .Include(s => s.CE_SupplierContacts)
                        .Include(s => s.CE_SupplierProducts)
                        .FirstOrDefaultAsync(s => s.Id == model.Id);

                    if (supplier == null)
                        return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier not found.");

                    supplier.Name = model.Name;
                    supplier.Status = model.Status;
                    supplier.Link = model.Link;

                    await db.SaveChangesAsync();

                    var submittedContactIds = model.Contacts.Where(c => c.Id > 0).Select(c => c.Id).ToList();
                    var contactsToDelete = supplier.CE_SupplierContacts
                        .Where(c => !submittedContactIds.Contains(c.Id))
                        .ToList();

                    db.CE_SupplierContacts.RemoveRange(contactsToDelete);
                    await db.SaveChangesAsync();

                    foreach (var contact in model.Contacts)
                    {
                        if (contact.Id > 0)
                        {
                            var existing = supplier.CE_SupplierContacts.FirstOrDefault(c => c.Id == contact.Id);
                            if (existing != null)
                            {
                                existing.ContactName = contact.ContactName;
                                existing.Role = contact.Role;
                                existing.PhoneNumber = contact.PhoneNumber;
                                existing.Email = contact.Email;
                                existing.Notes = contact.Notes;
                            }
                        }
                        else
                        {
                            db.CE_SupplierContacts.Add(new CE_SupplierContacts
                            {
                                SupplierId = supplier.Id,
                                ContactName = contact.ContactName,
                                Role = contact.Role,
                                PhoneNumber = contact.PhoneNumber,
                                Email = contact.Email,
                                Notes = contact.Notes
                            });
                        }
                    }

                    await db.SaveChangesAsync();

                    var submittedProductIds = model.Products.Where(p => p.Id > 0).Select(p => p.Id).ToList();

                    var productIdsInUseElectric = db.CE_ElectricContracts
                .Select(e => e.ProductId)
                .Distinct();

                    var productIdsInUseGas = db.CE_GasContracts
                        .Select(g => g.ProductId)
                        .Distinct();

                    var productIdsInUse = productIdsInUseElectric
                        .Union(productIdsInUseGas)
                        .Distinct()
                        .ToList();

                    var productsToDelete = supplier.CE_SupplierProducts
                        .Where(p => !submittedProductIds.Contains(p.Id))
                        .ToList();

                    var inUseProducts = productsToDelete
                        .Where(p => productIdsInUse.Contains(p.Id))
                        .ToList();

                    var deletableProducts = productsToDelete
                        .Where(p => !productIdsInUse.Contains(p.Id))
                        .ToList();

                    db.CE_SupplierProducts.RemoveRange(deletableProducts);
                    await db.SaveChangesAsync();

                    foreach (var product in model.Products)
                    {
                        if (product.Id > 0)
                        {
                            var existing = supplier.CE_SupplierProducts.FirstOrDefault(p => p.Id == product.Id);
                            if (existing != null)
                            {
                                existing.ProductName = product.ProductName;
                                existing.StartDate = product.StartDate;
                                existing.EndDate = product.EndDate;
                                existing.Commission = product.Commission;
                                existing.SupplierCommsType = product.SupplierCommsType;
                            }
                        }
                        else
                        {
                            db.CE_SupplierProducts.Add(new CE_SupplierProducts
                            {
                                SupplierId = supplier.Id,
                                ProductName = product.ProductName,
                                StartDate = product.StartDate,
                                EndDate = product.EndDate,
                                Commission = product.Commission,
                                SupplierCommsType = product.SupplierCommsType
                            });
                        }
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    if (inUseProducts.Any())
                    {
                        var names = string.Join(", ", inUseProducts.Select(p => p.ProductName));
                        return JsonResponse.Ok(new
                        {
                            success = true,
                            // partialWarning = true
                        }, $"Supplier updated successfully. However, the following product(s) could not be deleted as they are currently in use: {names} Please referesh the page");
                    }

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("Edit supplier failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while updating the supplier.");
                }
            }
        }

        #endregion

        #region getting_active_suppliers

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetActiveSuppliersForDropdown()
        {
            try
            {
                var today = DateTime.Today;

                var suppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .Include(s => s.CE_SupplierProducts)
                    .ToListAsync();

                var filtered = suppliers
                    .Where(s => s.CE_SupplierProducts.Any(p =>
                        !string.IsNullOrWhiteSpace(p.EndDate) &&
                        DateTime.TryParse(p.EndDate, out var endDate) &&
                        endDate > today))
                    .Select(s => new
                    {
                        s.Id,
                        Name = s.Name
                    })
                    .ToList();

                return JsonResponse.Ok(filtered);
            }
            catch (Exception ex)
            {
                Logger.Log("GetActiveSuppliersForDropdown failed: " + ex.Message);
                return JsonResponse.Fail("Unable to load active suppliers.");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetProductsBySupplier(long supplierId)
        {
            try
            {
                var today = DateTime.Today;

                var rawProducts = await db.CE_SupplierProducts
                    .Where(p => p.SupplierId == supplierId)
                    .ToListAsync();

                var products = rawProducts
                    .Where(p =>
                        !string.IsNullOrWhiteSpace(p.EndDate) &&
                        DateTime.Parse(p.EndDate) > today)
                    .Select(p => new { p.Id, ProductName = p.ProductName, SupplierCommsType = p.SupplierCommsType })
                    .ToList();

                return JsonResponse.Ok(products);
            }
            catch (Exception ex)
            {
                Logger.Log("GetProductsBySupplier: " + ex.Message);
                return JsonResponse.Fail("Unable to load products.");
            }
        }



        #endregion
    }
}