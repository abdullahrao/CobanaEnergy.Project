using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models.Sector;
using Logic;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Sector
{
    /// <summary>
    /// Handles Sector management functionality
    /// </summary>
    public class SectorController : BaseController
    {
        #region SectorDashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Dashboard()
        {
            try
            {
                // TODO: Replace with actual database query
                var sampleSectors = new List<SectorItemViewModel>
                {
                    new SectorItemViewModel
                    {
                        SectorId = "1",
                        Name = "ABC Brokerage Ltd",
                        Active = true,
                        StartDate = "2024-01-15",
                        EndDate = "",
                        Mobile = "+44 7700 900123",
                        SectorType = "Brokerage"
                    },
                    new SectorItemViewModel
                    {
                        SectorId = "2",
                        Name = "XYZ Closer Services",
                        Active = true,
                        StartDate = "2024-02-01",
                        EndDate = "",
                        Mobile = "+44 7700 900456",
                        SectorType = "Closer"
                    },
                    new SectorItemViewModel
                    {
                        SectorId = "3",
                        Name = "LeadGen Solutions",
                        Active = false,
                        StartDate = "2023-11-10",
                        EndDate = "2024-01-31",
                        Mobile = "+44 7700 900789",
                        SectorType = "Leads Generator"
                    },
                    new SectorItemViewModel
                    {
                        SectorId = "4",
                        Name = "Referral Partners UK",
                        Active = true,
                        StartDate = "2024-01-01",
                        EndDate = "",
                        Mobile = "+44 7700 900012",
                        SectorType = "Referral Partner"
                    },
                    new SectorItemViewModel
                    {
                        SectorId = "5",
                        Name = "Intro Services Ltd",
                        Active = true,
                        StartDate = "2024-03-01",
                        EndDate = "",
                        Mobile = "+44 7700 900345",
                        SectorType = "Introducer"
                    }
                };

                var model = new SectorDashboardViewModel
                {
                    Sectors = sampleSectors,
                    TotalSectors = sampleSectors.Count,
                    ActiveSectors = sampleSectors.Count(s => s.Active),
                    InactiveSectors = sampleSectors.Count(s => !s.Active)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load sector dashboard: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Sector Dashboard failed: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion

        #region CreateSector

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult Create()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load create sector page: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Create Sector page rendering failed!! {ex.Message}");
                return RedirectToAction("Dashboard", "Sector");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create(CreateSectorViewModel model)
        {
            try
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

                // TODO: Implement sector creation logic
                // This will involve saving to database, validation, etc.
                
                // Additional validation for Brokerage and Introducers
                if ((model.SectorType == "Brokerage" || model.SectorType == "Introducer") && 
                    string.IsNullOrWhiteSpace(model.OfgemID))
                {
                    return JsonResponse.Fail("Ofgem ID is required for Brokerage and Introducer sectors.");
                }

                // TODO: Implement sector creation logic
                // This will involve saving to database using the comprehensive model
                // The model now contains all the necessary data for:
                // - Sector details
                // - Bank details
                // - Company tax info
                // - Commission and payment details (based on sector type)
                // - Staff and sub-sections (based on sector type)
                
                // For now, just return success
                // Later we'll implement the actual database saving logic

                return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "Sector") }, "Sector created successfully!");
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Sector creation failed!! {ex.Message}");
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region EditSector

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Edit(string sectorId)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
            {
                TempData["ToastMessage"] = "Sector ID is required.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Dashboard", "Sector");
            }

            try
            {
                // TODO: Implement sector loading logic from database
                // For now, create a sample model with the new structure
                var model = new EditSectorViewModel
                {
                    SectorId = sectorId,
                    SectorType = "Brokerage", // Placeholder - will be populated with actual data
                    Name = "Sample Sector Name",
                    OfgemID = "OFG123456",
                    Active = true,
                    StartDate = "2024-01-01",
                    EndDate = "",
                    Mobile = "+44 7700 900123",
                    Email = "sample@example.com",
                    Address = "123 Sample Street",
                    Postcode = "AB12 3CD",
                    Notes = "Sample sector notes"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load sector: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Edit Sector GET failed!! {ex.Message}");
                return RedirectToAction("Dashboard", "Sector");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Edit(EditSectorViewModel model)
        {
            try
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

                // TODO: Implement sector update logic
                // This will involve updating database, validation, etc.

                return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "Sector") }, "Sector updated successfully!");
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Sector update failed!! {ex.Message}");
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion
    }
}
