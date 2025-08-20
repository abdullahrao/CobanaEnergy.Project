using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Sector;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.Commissions;
using CobanaEnergy.Project.Models.Sector.Brokerage;
using CobanaEnergy.Project.Models.Sector.ReferralPartner;
using CobanaEnergy.Project.Models.Sector.Introducer;
using Logic;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        private readonly ApplicationDBContext db;

        public SectorController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region SectorDashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Dashboard()
        {
            try
            {
                // Load data once when page loads - more efficient than real-time
                var sectors = await db.CE_Sector
                    .OrderByDescending(s => s.SectorID)
                    .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType
                }).ToList();

                var model = new SectorDashboardViewModel
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
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

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSectorsByType(string sectorType)
        {
            try
            {
                // Only use AJAX for filtering when user actually changes the filter
                if (string.IsNullOrEmpty(sectorType))
                {
                    return JsonResponse.Fail("Sector type is required for filtering.");
                }

                var sectors = await db.CE_Sector
                    .Where(s => s.SectorType == sectorType)
                    .OrderByDescending(s => s.SectorID)
                    .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType
                }).ToList();

                // Return just the filtered data, not the full model
                return JsonResponse.Ok(new
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sectors by type: {ex.Message}");
                return JsonResponse.Fail("Failed to load sectors.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSectorStatistics()
        {
            try
            {
                var totalSectors = await db.CE_Sector.CountAsync();
                var activeSectors = await db.CE_Sector.CountAsync(s => s.Active);
                var inactiveSectors = await db.CE_Sector.CountAsync(s => !s.Active);

                var sectorTypeStats = await db.CE_Sector
                    .GroupBy(s => s.SectorType)
                    .Select(g => new
                    {
                        SectorType = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(s => s.Active)
                    })
                    .ToListAsync();

                var stats = new
                {
                    TotalSectors = totalSectors,
                    ActiveSectors = activeSectors,
                    InactiveSectors = inactiveSectors,
                    SectorTypeBreakdown = sectorTypeStats
                };

                return JsonResponse.Ok(stats);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sector statistics: {ex.Message}");
                return JsonResponse.Fail("Failed to load sector statistics.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> RefreshDashboardData()
        {
            try
            {
                // This method can be called after CRUD operations to refresh the dashboard
                var sectors = await db.CE_Sector
                    .OrderByDescending(s => s.SectorID)
                    .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType
                }).ToList();

                var model = new SectorDashboardViewModel
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                };

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to refresh dashboard data: {ex.Message}");
                return JsonResponse.Fail("Failed to refresh dashboard data.");
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
        public async Task<ActionResult> Create(CreateSectorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ToastMessage"] = string.Join("<br>", errors);
                TempData["ToastType"] = "error";
                return View(model);
            }

            // Additional validation for Brokerage and Introducers
            if ((model.SectorType == "Brokerage" || model.SectorType == "Introducer") && 
                string.IsNullOrWhiteSpace(model.OfgemID))
            {
                TempData["ToastMessage"] = "Ofgem ID is required for Brokerage and Introducer sectors.";
                TempData["ToastType"] = "error";
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Step 1: Create the main sector
                    var sector = new CE_Sector
                    {
                        Name = model.Name,
                        Active = model.Active,
                        StartDate = !string.IsNullOrEmpty(model.StartDate) ? DateTime.Parse(model.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(model.EndDate) ? DateTime.Parse(model.EndDate) : (DateTime?)null,
                        Email = model.Email,
                        Landline = model.Landline,
                        Mobile = model.Mobile,
                        OfgemID = model.OfgemID,
                        Department = model.Department,
                        SectorType = model.SectorType
                    };

                    db.CE_Sector.Add(sector);
                    await db.SaveChangesAsync();

                    // Step 2: Add Bank Details if provided
                    if (model.BankDetails != null && !string.IsNullOrEmpty(model.BankDetails.BankName))
                    {
                        var bankDetails = new CE_BankDetails
                        {
                            EntityType = "Sector",
                            EntityID = sector.SectorID,
                            BankName = model.BankDetails.BankName,
                            BankBranchAddress = model.BankDetails.BankBranchAddress,
                            ReceiversAddress = model.BankDetails.ReceiversAddress,
                            AccountName = model.BankDetails.AccountName,
                            AccountSortCode = model.BankDetails.AccountSortCode,
                            AccountNumber = model.BankDetails.AccountNumber,
                            IBAN = model.BankDetails.IBAN,
                            SwiftCode = model.BankDetails.SwiftCode
                        };
                        db.CE_BankDetails.Add(bankDetails);
                    }

                    await db.SaveChangesAsync();

                    // Step 3: Add Company Tax Info if provided
                    if (model.CompanyTaxInfo != null && !string.IsNullOrEmpty(model.CompanyTaxInfo.CompanyRegistration))
                    {
                        var companyTaxInfo = new CE_CompanyTaxInfo
                        {
                            EntityType = "Sector",
                            EntityID = sector.SectorID,
                            CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration,
                            VATNumber = model.CompanyTaxInfo.VATNumber,
                            Notes = model.CompanyTaxInfo.Notes
                        };
                        db.CE_CompanyTaxInfo.Add(companyTaxInfo);
                    }
                    await db.SaveChangesAsync();


                    // Step 4: Add Commission and Payment records based on sector type
                    await AddCommissionRecords(sector.SectorID, model);

                    // Step 5: Add Staff and Sub-sections based on sector type
                    await AddStaffAndSubsections(sector.SectorID, model);

                    // Step 6: Add Bank Details and Company Tax Info for sub-entities
                    await AddSubEntityBankDetailsAndTaxInfo(model);

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    TempData["ToastMessage"] = "Sector created successfully!";
                    TempData["ToastType"] = "success";
                    return RedirectToAction("Dashboard", "Sector");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logic.Logger.Log($"Sector creation failed: {ex.Message}");
                    TempData["ToastMessage"] = "An unexpected error occurred while saving sector.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }
            }
        }

        private async Task AddCommissionRecords(int sectorId, CreateSectorViewModel model)
        {
            // Add Brokerage Commissions
            if (model.BrokerageCommissions != null && model.BrokerageCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.BrokerageCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var brokerageCommission = new CE_BrokerageCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,  // ViewModel.Commission -> DBModel.Commission
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_BrokerageCommissionAndPayment.Add(brokerageCommission);
                }
            }

            // Add Closer Commissions
            if (model.CloserCommissions != null && model.CloserCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.CloserCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var closerCommission = new CE_CloserCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,  // ViewModel.Commission -> DBModel.Commission
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_CloserCommissionAndPayment.Add(closerCommission);
                }
            }

            // Add Introducer Commissions
            if (model.IntroducerCommissions != null && model.IntroducerCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.IntroducerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var introducerCommission = new CE_IntroducerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        CommissionPercent = commission.Commission,  // ViewModel.Commission -> DBModel.CommissionPercent
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_IntroducerCommissionAndPayment.Add(introducerCommission);
                }
            }

            // Add Referral Partner Commissions
            if (model.ReferralPartnerCommissions != null && model.ReferralPartnerCommissions.Any(c => !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
            {
                foreach (var commission in model.ReferralPartnerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
                {
                    var referralCommission = new CE_ReferralPartnerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        ReferralPartnerCommission = commission.ReferralPartnerCommission,
                        ReferralPartnerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                        ReferralPartnerEndDate = !string.IsNullOrEmpty(commission.ReferralPartnerEndDate) ? DateTime.Parse(commission.ReferralPartnerEndDate) : (DateTime?)null,
                        BrokerageCommission = commission.BrokerageCommission,
                        BrokerageStartDate = !string.IsNullOrEmpty(commission.BrokerageStartDate) ? DateTime.Parse(commission.BrokerageStartDate) : (DateTime?)null,
                        BrokerageEndDate = !string.IsNullOrEmpty(commission.BrokerageEndDate) ? DateTime.Parse(commission.BrokerageEndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_ReferralPartnerCommissionAndPayment.Add(referralCommission);
                }
            }

            // Add Lead Generator Commissions
            if (model.LeadGeneratorCommissions != null && model.LeadGeneratorCommissions.Any(c => !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
            {
                foreach (var commission in model.LeadGeneratorCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
                {
                    var leadGenCommission = new CE_LeadGeneratorCommissionAndPayment
                    {
                        SectorID = sectorId,
                        LeadGeneratorCommissionPercent = commission.LeadGeneratorCommission,
                        LeadGeneratorStartDate = !string.IsNullOrEmpty(commission.LeadGeneratorStartDate) ? DateTime.Parse(commission.LeadGeneratorStartDate) : (DateTime?)null,
                        LeadGeneratorEndDate = !string.IsNullOrEmpty(commission.LeadGeneratorEndDate) ? DateTime.Parse(commission.LeadGeneratorEndDate) : (DateTime?)null,
                        CloserCommissionPercent = commission.CloserCommission,
                        CloserStartDate = !string.IsNullOrEmpty(commission.CloserStartDate) ? DateTime.Parse(commission.CloserStartDate) : (DateTime?)null,
                        CloserEndDate = !string.IsNullOrEmpty(commission.CloserEndDate) ? DateTime.Parse(commission.CloserEndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_LeadGeneratorCommissionAndPayment.Add(leadGenCommission);
                }
            }
        }

        private async Task AddStaffAndSubsections(int sectorId, CreateSectorViewModel model)
        {
            // Add Brokerage Staff
            if (model.BrokerageStaff != null && model.BrokerageStaff.Any(s => !string.IsNullOrEmpty(s.BrokerageStaffName)))
            {
                foreach (var staff in model.BrokerageStaff.Where(s => s != null && !string.IsNullOrEmpty(s.BrokerageStaffName)))
                {
                    var brokerageStaff = new CE_BrokerageStaff
                    {
                        SectorID = sectorId,
                        BrokerageStaffName = staff.BrokerageStaffName,
                        Active = staff.Active,
                        StartDate = !string.IsNullOrEmpty(staff.StartDate) ? DateTime.Parse(staff.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(staff.EndDate) ? DateTime.Parse(staff.EndDate) : (DateTime?)null,
                        Email = staff.Email ?? "",
                        Landline = staff.Landline ?? "",
                        Mobile = staff.Mobile ?? "",
                        Notes = staff.Notes ?? ""
                    };
                    db.CE_BrokerageStaff.Add(brokerageStaff);
                }
            }

            // Add Sub Brokerages and their commissions
            if (model.SubBrokerages != null && model.SubBrokerages.Any(s => !string.IsNullOrEmpty(s.SubBrokerageName)))
            {
                foreach (var subBrokerage in model.SubBrokerages.Where(s => s != null && !string.IsNullOrEmpty(s.SubBrokerageName)))
                {
                    var subBrokerageEntity = new CE_SubBrokerage
                    {
                        SectorID = sectorId,
                        SubBrokerageName = subBrokerage.SubBrokerageName,
                        OfgemID = subBrokerage.OfgemID ?? "",
                        Active = subBrokerage.Active,
                        StartDate = !string.IsNullOrEmpty(subBrokerage.StartDate) ? DateTime.Parse(subBrokerage.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subBrokerage.EndDate) ? DateTime.Parse(subBrokerage.EndDate) : (DateTime?)null,
                        Email = subBrokerage.Email ?? "",
                        Landline = subBrokerage.Landline ?? "",
                        Mobile = subBrokerage.Mobile ?? ""
                    };
                    db.CE_SubBrokerage.Add(subBrokerageEntity);
                    
                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();
                    
                    // Add Sub Brokerage Commissions using the actual ID
                    if (subBrokerage.Commissions != null && subBrokerage.Commissions.Any(c => !string.IsNullOrEmpty(c.SubBrokerageCommission.ToString())))
                    {
                        foreach (var commission in subBrokerage.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubBrokerageCommission.ToString())))
                        {
                            var subBrokerageCommission = new CE_SubBrokerageCommissionAndPayment
                            {
                                SubBrokerageID = subBrokerageEntity.SubBrokerageID, // Use actual ID, not 0
                                SubBrokerageCommissionPercent = commission.SubBrokerageCommission,
                                SubBrokerageStartDate = !string.IsNullOrEmpty(commission.SubBrokerageStartDate) ? DateTime.Parse(commission.SubBrokerageStartDate) : (DateTime?)null,
                                SubBrokerageEndDate = !string.IsNullOrEmpty(commission.SubBrokerageEndDate) ? DateTime.Parse(commission.SubBrokerageEndDate) : (DateTime?)null,
                                BrokerageCommissionPercent = commission.BrokerageCommission,
                                BrokerageStartDate = !string.IsNullOrEmpty(commission.BrokerageStartDate) ? DateTime.Parse(commission.BrokerageStartDate) : (DateTime?)null,
                                BrokerageEndDate = !string.IsNullOrEmpty(commission.BrokerageEndDate) ? DateTime.Parse(commission.BrokerageEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubBrokerageCommissionAndPayment.Add(subBrokerageCommission);
                        }
                    }
                }
            }

            // Add Sub Referrals and their commissions
            if (model.SubReferrals != null && model.SubReferrals.Any(s => !string.IsNullOrEmpty(s.SubReferralPartnerName)))
            {
                foreach (var subReferral in model.SubReferrals.Where(s => s != null && !string.IsNullOrEmpty(s.SubReferralPartnerName)))
                {
                    var subReferralEntity = new CE_SubReferral
                    {
                        SectorID = sectorId,
                        SubReferralPartnerName = subReferral.SubReferralPartnerName,
                        Active = subReferral.Active,
                        StartDate = !string.IsNullOrEmpty(subReferral.StartDate) ? DateTime.Parse(subReferral.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subReferral.EndDate) ? DateTime.Parse(subReferral.EndDate) : (DateTime?)null,
                        SubReferralPartnerEmail = subReferral.Email ?? "",
                        SubReferralPartnerLandline = subReferral.Landline ?? "",
                        SubReferralPartnerMobile = subReferral.Mobile ?? ""
                    };
                    db.CE_SubReferral.Add(subReferralEntity);
                    
                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();
                    
                    // Add Sub Referral Commissions using the actual ID
                    if (subReferral.Commissions != null && subReferral.Commissions.Any(c => !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                    {
                        foreach (var commission in subReferral.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                        {
                            var subReferralCommission = new CE_SubReferralCommissionAndPayment
                            {
                                SubReferralID = subReferralEntity.SubReferralID, // Use actual ID, not 0
                                SubIntroducerCommission = commission.SubIntroducerCommission,
                                SubIntroducerStartDate = !string.IsNullOrEmpty(commission.SubIntroducerStartDate) ? DateTime.Parse(commission.SubIntroducerStartDate) : (DateTime?)null,
                                SubIntroducerEndDate = !string.IsNullOrEmpty(commission.SubIntroducerEndDate) ? DateTime.Parse(commission.SubIntroducerEndDate) : (DateTime?)null,
                                IntroducerCommission = commission.IntroducerCommission,
                                IntroducerStartDate = !string.IsNullOrEmpty(commission.IntroducerStartDate) ? DateTime.Parse(commission.IntroducerStartDate) : (DateTime?)null,
                                IntroducerEndDate = !string.IsNullOrEmpty(commission.IntroducerEndDate) ? DateTime.Parse(commission.IntroducerEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubReferralCommissionAndPayment.Add(subReferralCommission);
                        }
                    }
                }
            }

            // Add Sub Introducers and their commissions
            if (model.SubIntroducers != null && model.SubIntroducers.Any(s => !string.IsNullOrEmpty(s.SubIntroducerName)))
            {
                foreach (var subIntroducer in model.SubIntroducers.Where(s => s != null && !string.IsNullOrEmpty(s.SubIntroducerName)))
                {
                    var subIntroducerEntity = new CE_SubIntroducer
                    {
                        SectorID = sectorId,
                        SubIntroducerName = subIntroducer.SubIntroducerName,
                        OfgemID = subIntroducer.OfgemID ?? "",
                        Active = subIntroducer.Active,
                        StartDate = !string.IsNullOrEmpty(subIntroducer.StartDate) ? DateTime.Parse(subIntroducer.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subIntroducer.EndDate) ? DateTime.Parse(subIntroducer.EndDate) : (DateTime?)null,
                        SubIntroducerEmail = subIntroducer.Email ?? "",
                        SubIntroducerLandline = subIntroducer.Landline ?? "",
                        SubIntroducerMobile = subIntroducer.Mobile ?? ""
                    };
                    db.CE_SubIntroducer.Add(subIntroducerEntity);
                    
                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();
                    
                    // Add Sub Introducer Commissions using the actual ID
                    if (subIntroducer.Commissions != null && subIntroducer.Commissions.Any(c => !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                    {
                        foreach (var commission in subIntroducer.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                        {
                            var subIntroducerCommission = new CE_SubIntroducerCommissionAndPayment
                            {
                                SubIntroducerID = subIntroducerEntity.SubIntroducerID, // Use actual ID, not 0
                                SubIntroducerCommission = commission.SubIntroducerCommission,
                                SubIntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.SubIntroducerStartDate) ? DateTime.Parse(commission.SubIntroducerStartDate) : (DateTime?)null,
                                SubIntroducerCommissionEndDate = !string.IsNullOrEmpty(commission.SubIntroducerEndDate) ? DateTime.Parse(commission.SubIntroducerEndDate) : (DateTime?)null,
                                IntroducerCommission = commission.IntroducerCommission,
                                IntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.IntroducerStartDate) ? DateTime.Parse(commission.IntroducerStartDate) : (DateTime?)null,
                                IntroducerCommissionEndDate = !string.IsNullOrEmpty(commission.IntroducerEndDate) ? DateTime.Parse(commission.IntroducerEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubIntroducerCommissionAndPayment.Add(subIntroducerCommission);
                        }
                    }
                }
            }
        }

        private async Task AddSubEntityBankDetailsAndTaxInfo(CreateSectorViewModel model)
        {
            // Add Sub Brokerage Bank Details and Company Tax Info
            if (model.SubBrokerages != null && model.SubBrokerages.Any(s => !string.IsNullOrEmpty(s.SubBrokerageName)))
            {
                foreach (var subBrokerage in model.SubBrokerages.Where(s => s != null && !string.IsNullOrEmpty(s.SubBrokerageName)))
                {
                    // Find the existing sub-brokerage entity to get its ID
                    var existingSubBrokerage = await db.CE_SubBrokerage
                        .FirstOrDefaultAsync(sb => sb.SubBrokerageName == subBrokerage.SubBrokerageName);
                    
                    if (existingSubBrokerage != null)
                    {
                        // Add Bank Details if provided
                        if (subBrokerage.BankDetails != null && !string.IsNullOrEmpty(subBrokerage.BankDetails.BankName))
                        {
                            var subBrokerageBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubBrokerage",
                                EntityID = existingSubBrokerage.SubBrokerageID,
                                BankName = subBrokerage.BankDetails.BankName,
                                BankBranchAddress = subBrokerage.BankDetails.BankBranchAddress,
                                ReceiversAddress = subBrokerage.BankDetails.ReceiversAddress,
                                AccountName = subBrokerage.BankDetails.AccountName,
                                AccountSortCode = subBrokerage.BankDetails.AccountSortCode,
                                AccountNumber = subBrokerage.BankDetails.AccountNumber,
                                IBAN = subBrokerage.BankDetails.IBAN,
                                SwiftCode = subBrokerage.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subBrokerageBankDetails);
                        }

                        // Add Company Tax Info if provided
                        if (subBrokerage.CompanyTaxInfo != null && !string.IsNullOrEmpty(subBrokerage.CompanyTaxInfo.CompanyRegistration))
                        {
                            var subBrokerageCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubBrokerage",
                                EntityID = existingSubBrokerage.SubBrokerageID,
                                CompanyRegistration = subBrokerage.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subBrokerage.CompanyTaxInfo.VATNumber,
                                Notes = subBrokerage.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subBrokerageCompanyTaxInfo);
                        }
                    }
                }
            }

            // Add Sub Referral Bank Details and Company Tax Info
            if (model.SubReferrals != null && model.SubReferrals.Any(s => !string.IsNullOrEmpty(s.SubReferralPartnerName)))
            {
                foreach (var subReferral in model.SubReferrals.Where(s => s != null && !string.IsNullOrEmpty(s.SubReferralPartnerName)))
                {
                    // Find the existing sub-referral entity to get its ID
                    var existingSubReferral = await db.CE_SubReferral
                        .FirstOrDefaultAsync(sr => sr.SubReferralPartnerName == subReferral.SubReferralPartnerName);
                    
                    if (existingSubReferral != null)
                    {
                        // Add Bank Details if provided
                        if (subReferral.BankDetails != null && !string.IsNullOrEmpty(subReferral.BankDetails.BankName))
                        {
                            var subReferralBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubReferral",
                                EntityID = existingSubReferral.SubReferralID,
                                BankName = subReferral.BankDetails.BankName,
                                BankBranchAddress = subReferral.BankDetails.BankBranchAddress,
                                ReceiversAddress = subReferral.BankDetails.ReceiversAddress,
                                AccountName = subReferral.BankDetails.AccountName,
                                AccountSortCode = subReferral.BankDetails.AccountSortCode,
                                AccountNumber = subReferral.BankDetails.AccountNumber,
                                IBAN = subReferral.BankDetails.IBAN,
                                SwiftCode = subReferral.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subReferralBankDetails);
                        }

                        // Add Company Tax Info if provided
                        if (subReferral.CompanyTaxInfo != null && !string.IsNullOrEmpty(subReferral.CompanyTaxInfo.CompanyRegistration))
                        {
                            var subReferralCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubReferral",
                                EntityID = existingSubReferral.SubReferralID,
                                CompanyRegistration = subReferral.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subReferral.CompanyTaxInfo.VATNumber,
                                Notes = subReferral.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subReferralCompanyTaxInfo);
                        }
                    }
                }
            }

            // Add Sub Introducer Bank Details and Company Tax Info
            if (model.SubIntroducers != null && model.SubIntroducers.Any(s => !string.IsNullOrEmpty(s.SubIntroducerName)))
            {
                foreach (var subIntroducer in model.SubIntroducers.Where(s => s != null && !string.IsNullOrEmpty(s.SubIntroducerName)))
                {
                    // Find the existing sub-introducer entity to get its ID
                    var existingSubIntroducer = await db.CE_SubIntroducer
                        .FirstOrDefaultAsync(si => si.SubIntroducerName == subIntroducer.SubIntroducerName);
                    
                    if (existingSubIntroducer != null)
                    {
                        // Add Bank Details if provided
                        if (subIntroducer.BankDetails != null && !string.IsNullOrEmpty(subIntroducer.BankDetails.BankName))
                        {
                            var subIntroducerBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubIntroducer",
                                EntityID = existingSubIntroducer.SubIntroducerID,
                                BankName = subIntroducer.BankDetails.BankName,
                                BankBranchAddress = subIntroducer.BankDetails.BankBranchAddress,
                                ReceiversAddress = subIntroducer.BankDetails.ReceiversAddress,
                                AccountName = subIntroducer.BankDetails.AccountName,
                                AccountSortCode = subIntroducer.BankDetails.AccountSortCode,
                                AccountNumber = subIntroducer.BankDetails.AccountNumber,
                                IBAN = subIntroducer.BankDetails.IBAN,
                                SwiftCode = subIntroducer.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subIntroducerBankDetails);
                        }

                        // Add Company Tax Info if provided
                        if (subIntroducer.CompanyTaxInfo != null && !string.IsNullOrEmpty(subIntroducer.CompanyTaxInfo.CompanyRegistration))
                        {
                            var subIntroducerCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubIntroducer",
                                EntityID = existingSubIntroducer.SubIntroducerID,
                                CompanyRegistration = subIntroducer.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subIntroducer.CompanyTaxInfo.VATNumber,
                                Notes = subIntroducer.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subIntroducerCompanyTaxInfo);
                        }
                    }
                }
            }
        }

        #endregion

        #region EditSector

        [Authorize(Roles = "Controls")]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Logic.Logger.Log("Sector ID is null or empty - redirecting to dashboard");
                TempData["ToastMessage"] = "Sector ID is required.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Dashboard", "Sector");
            }

            try
            {
                if (!int.TryParse(id, out int sectorId))
                {
                    Logic.Logger.Log($"Failed to parse id '{id}' to int");
                    TempData["ToastMessage"] = "Invalid sector ID.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Dashboard", "Sector");
                }
                
                Logic.Logger.Log($"Successfully parsed id '{id}' to int: {sectorId}");

                var sector = db.CE_Sector
                    .Include(s => s.CE_BrokerageStaff)
                    .Include(s => s.CE_SubBrokerages)
                    .Include(s => s.CE_SubReferrals)
                    .Include(s => s.CE_SubIntroducers)
                    .FirstOrDefault(s => s.SectorID == sectorId);

                if (sector == null)
                {
                    Logic.Logger.Log($"Sector with ID {sectorId} not found in database");
                    TempData["ToastMessage"] = "Sector not found.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Dashboard", "Sector");
                }
                
                Logic.Logger.Log($"Found sector: {sector.Name} (ID: {sector.SectorID})");

                // Get Bank Details
                var bankDetails = db.CE_BankDetails
                    .Where(b => b.EntityType == "Sector" && b.EntityID == sector.SectorID)
                    .FirstOrDefault();

                // Get Company Tax Info
                var companyTaxInfo = db.CE_CompanyTaxInfo
                    .Where(c => c.EntityType == "Sector" && c.EntityID == sector.SectorID)
                    .FirstOrDefault();

                // Get Commission Records
                var brokerageCommissions = db.CE_BrokerageCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var closerCommissions = db.CE_CloserCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var introducerCommissions = db.CE_IntroducerCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var referralPartnerCommissions = db.CE_ReferralPartnerCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var leadGeneratorCommissions = db.CE_LeadGeneratorCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                // Build the complete EditSectorViewModel
                var model = BuildEditSectorViewModel(sector, bankDetails, companyTaxInfo, 
                    brokerageCommissions, closerCommissions, introducerCommissions, 
                    referralPartnerCommissions, leadGeneratorCommissions);

                Logic.Logger.Log($"Successfully built EditSectorViewModel for sector {sector.SectorID}");
                return View(model);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sector for edit: {ex.Message}");
                TempData["ToastMessage"] = "Failed to load sector details.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Dashboard", "Sector");
            }
        }

        private EditSectorViewModel BuildEditSectorViewModel(CE_Sector sector, 
            CE_BankDetails bankDetails, CE_CompanyTaxInfo companyTaxInfo,
            List<CE_BrokerageCommissionAndPayment> brokerageCommissions,
            List<CE_CloserCommissionAndPayment> closerCommissions,
            List<CE_IntroducerCommissionAndPayment> introducerCommissions,
            List<CE_ReferralPartnerCommissionAndPayment> referralPartnerCommissions,
            List<CE_LeadGeneratorCommissionAndPayment> leadGeneratorCommissions)
        {
            var model = new EditSectorViewModel
            {
                SectorId = sector.SectorID.ToString(),
                SectorType = sector.SectorType,
                Name = sector.Name,
                OfgemID = sector.OfgemID,
                Active = sector.Active,
                StartDate = sector.StartDate?.ToString("yyyy-MM-dd") ?? "",
                EndDate = sector.EndDate?.ToString("yyyy-MM-dd") ?? "",
                Mobile = sector.Mobile ?? "",
                Email = sector.Email ?? "",
                Landline = sector.Landline ?? "",
                Department = sector.Department ?? ""
            };

            // Set Bank Details
            if (bankDetails != null)
            {
                model.BankDetails = new BankDetailsViewModel
                {
                    BankName = bankDetails.BankName ?? "",
                    BankBranchAddress = bankDetails.BankBranchAddress ?? "",
                    ReceiversAddress = bankDetails.ReceiversAddress ?? "",
                    AccountName = bankDetails.AccountName ?? "",
                    AccountSortCode = bankDetails.AccountSortCode ?? "",
                    AccountNumber = bankDetails.AccountNumber ?? "",
                    IBAN = bankDetails.IBAN ?? "",
                    SwiftCode = bankDetails.SwiftCode ?? ""
                };
            }

            // Set Company Tax Info
            if (companyTaxInfo != null)
            {
                model.CompanyTaxInfo = new CompanyTaxInfoViewModel
                {
                    CompanyRegistration = companyTaxInfo.CompanyRegistration ?? "",
                    VATNumber = companyTaxInfo.VATNumber ?? "",
                    Notes = companyTaxInfo.Notes ?? ""
                };
            }

            // Set Commission Records
            if (brokerageCommissions.Any())
            {
                model.BrokerageCommissions = brokerageCommissions.Select(c => new BrokerageCommissionAndPaymentViewModel
                {
                    Commission = c.Commission,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (closerCommissions.Any())
            {
                model.CloserCommissions = closerCommissions.Select(c => new CloserCommissionAndPaymentViewModel
                {
                    Commission = c.Commission,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (introducerCommissions.Any())
            {
                model.IntroducerCommissions = introducerCommissions.Select(c => new IntroducerCommissionAndPaymentViewModel
                {
                    Commission = c.CommissionPercent,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (referralPartnerCommissions.Any())
            {
                model.ReferralPartnerCommissions = referralPartnerCommissions.Select(c => new ReferralPartnerCommissionAndPaymentViewModel
                {
                    ReferralPartnerCommission = c.ReferralPartnerCommission,
                    ReferralPartnerStartDate = c.ReferralPartnerStartDate?.ToString("yyyy-MM-dd") ?? "",
                    ReferralPartnerEndDate = c.ReferralPartnerEndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (leadGeneratorCommissions.Any())
            {
                model.LeadGeneratorCommissions = leadGeneratorCommissions.Select(c => new LeadGeneratorCommissionAndPaymentViewModel
                {
                    LeadGeneratorCommission = c.LeadGeneratorCommissionPercent,
                    LeadGeneratorStartDate = c.LeadGeneratorStartDate?.ToString("yyyy-MM-dd") ?? "",
                    LeadGeneratorEndDate = c.LeadGeneratorEndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            // Set Staff and Sub-sections
            if (sector.CE_BrokerageStaff.Any())
            {
                model.BrokerageStaff = sector.CE_BrokerageStaff.Select(s => new BrokerageStaffViewModel
                {
                    BrokerageStaffName = s.BrokerageStaffName ?? "",
                    Email = s.Email ?? "",
                    Mobile = s.Mobile ?? "",
                    Landline = s.Landline ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active
                }).ToList();
            }

            if (sector.CE_SubBrokerages.Any())
            {
                model.SubBrokerages = sector.CE_SubBrokerages.Select(s => new SubBrokerageViewModel
                {
                    SubBrokerageName = s.SubBrokerageName ?? "",
                    OfgemID = s.OfgemID ?? "",
                    Email = s.Email ?? "",
                    Landline = s.Landline ?? "",
                    Mobile = s.Mobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active
                }).ToList();
            }

            if (sector.CE_SubReferrals.Any())
            {
                model.SubReferrals = sector.CE_SubReferrals.Select(s => new SubReferralViewModel
                {
                    SubReferralPartnerName = s.SubReferralPartnerName ?? "",
                    Email = s.SubReferralPartnerEmail ?? "",
                    Landline = s.SubReferralPartnerLandline ?? "",
                    Mobile = s.SubReferralPartnerMobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active
                }).ToList();
            }

            if (sector.CE_SubIntroducers.Any())
            {
                model.SubIntroducers = sector.CE_SubIntroducers.Select(s => new SubIntroducerViewModel
                {
                    SubIntroducerName = s.SubIntroducerName ?? "",
                    OfgemID = s.OfgemID ?? "",
                    Email = s.SubIntroducerEmail ?? "",
                    Landline = s.SubIntroducerLandline ?? "",
                    Mobile = s.SubIntroducerMobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active
                }).ToList();
            }

            return model;
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditSectorViewModel model)
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
                    TempData["ToastMessage"] = combinedError;
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Edit", new { id = model.SectorId });
                }

                // Additional validation for Brokerage and Introducers
                if ((model.SectorType == "Brokerage" || model.SectorType == "Introducer") && 
                    string.IsNullOrWhiteSpace(model.OfgemID))
                {
                    TempData["ToastMessage"] = "Ofgem ID is required for Brokerage and Introducer sectors.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Edit", new { id = model.SectorId });
                }

                if (!int.TryParse(model.SectorId, out int sectorId))
                {
                    TempData["ToastMessage"] = "Invalid sector ID.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Dashboard", "Sector");
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Update the main sector
                        var sector = await db.CE_Sector.FindAsync(sectorId);
                        if (sector == null)
                        {
                            return JsonResponse.Fail("Sector not found.");
                        }

                        sector.Name = model.Name;
                        sector.Active = model.Active;
                        sector.StartDate = !string.IsNullOrEmpty(model.StartDate) ? DateTime.Parse(model.StartDate) : (DateTime?)null;
                        sector.EndDate = !string.IsNullOrEmpty(model.EndDate) ? DateTime.Parse(model.EndDate) : (DateTime?)null;
                        sector.Email = model.Email;
                        sector.Mobile = model.Mobile;
                        sector.OfgemID = model.OfgemID;
                        sector.SectorType = model.SectorType;
                        sector.Landline = model.Landline;
                        sector.Department = model.Department;

                        // Step 2: Update Bank Details
                        var existingBankDetails = await db.CE_BankDetails
                            .Where(b => b.EntityType == "Sector" && b.EntityID == sectorId)
                            .FirstOrDefaultAsync();

                        if (existingBankDetails != null)
                        {
                            // Update existing
                            existingBankDetails.BankName = model.BankDetails.BankName;
                            existingBankDetails.BankBranchAddress = model.BankDetails.BankBranchAddress;
                            existingBankDetails.ReceiversAddress = model.BankDetails.ReceiversAddress;
                            existingBankDetails.AccountName = model.BankDetails.AccountName;
                            existingBankDetails.AccountSortCode = model.BankDetails.AccountSortCode;
                            existingBankDetails.AccountNumber = model.BankDetails.AccountNumber;
                            existingBankDetails.IBAN = model.BankDetails.IBAN;
                            existingBankDetails.SwiftCode = model.BankDetails.SwiftCode;
                        }
                        else if (!string.IsNullOrEmpty(model.BankDetails.BankName))
                        {
                            // Create new
                            var bankDetails = new CE_BankDetails
                            {
                                EntityType = "Sector",
                                EntityID = sectorId,
                                BankName = model.BankDetails.BankName,
                                BankBranchAddress = model.BankDetails.BankBranchAddress,
                                ReceiversAddress = model.BankDetails.ReceiversAddress,
                                AccountName = model.BankDetails.AccountName,
                                AccountSortCode = model.BankDetails.AccountSortCode,
                                AccountNumber = model.BankDetails.AccountNumber,
                                IBAN = model.BankDetails.IBAN,
                                SwiftCode = model.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(bankDetails);
                        }

                        // Step 3: Update Company Tax Info
                        var existingCompanyTaxInfo = await db.CE_CompanyTaxInfo
                            .Where(c => c.EntityType == "Sector" && c.EntityID == sectorId)
                            .FirstOrDefaultAsync();

                        if (existingCompanyTaxInfo != null)
                        {
                            // Update existing
                            existingCompanyTaxInfo.CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration;
                            existingCompanyTaxInfo.VATNumber = model.CompanyTaxInfo.VATNumber;
                            existingCompanyTaxInfo.Notes = model.CompanyTaxInfo.Notes;
                        }
                        else if (!string.IsNullOrEmpty(model.CompanyTaxInfo.CompanyRegistration))
                        {
                            // Create new
                            var companyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "Sector",
                                EntityID = sectorId,
                                CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = model.CompanyTaxInfo.VATNumber,
                                Notes = model.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(companyTaxInfo);
                        }

                        await db.SaveChangesAsync();

                        // Step 4: Update Commission Records
                        await UpdateCommissionRecords(sectorId, model);

                        // Step 5: Update Staff and Sub-sections
                        await UpdateStaffAndSubsections(sectorId, model);

                        await db.SaveChangesAsync();
                        transaction.Commit();

                        TempData["ToastMessage"] = "Sector updated successfully!";
                    TempData["ToastType"] = "success";
                    return RedirectToAction("Dashboard", "Sector");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logic.Logger.Log($"Sector update transaction failed: {ex.Message}");
                        TempData["ToastMessage"] = $"Failed to update sector: {ex.Message}";
                        TempData["ToastType"] = "error";
                        return RedirectToAction("Edit", new { id = model.SectorId });
                    }
                }
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Sector update failed!! {ex.Message}");
                TempData["ToastMessage"] = $"An error occurred: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Edit", new { id = model.SectorId });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> DeleteSector(string sectorId)
        {
            try
            {
                if (string.IsNullOrEmpty(sectorId) || !int.TryParse(sectorId, out int id))
                    return JsonResponse.Fail("Invalid or missing sector ID.");

                var sector = await db.CE_Sector.FindAsync(id);
                if (sector == null)
                    return JsonResponse.Fail("Sector not found.");

                // Soft delete - set Active to false
                sector.Active = false;
                sector.EndDate = DateTime.Now;

                await db.SaveChangesAsync();

                return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "Sector") }, "Sector deleted successfully!");
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to delete sector: {ex.Message}");
                return JsonResponse.Fail("Failed to delete sector.");
            }
        }

        #endregion

        #region Helper Methods for Edit

        private async Task UpdateCommissionRecords(int sectorId, EditSectorViewModel model)
        {
            // Update Brokerage Commissions - Only for Brokerage sector type
            if (model.SectorType == "Brokerage" && model.BrokerageCommissions != null && model.BrokerageCommissions.Any())
            {
                // Remove existing
                var existingBrokerageCommissions = await db.CE_BrokerageCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();
                db.CE_BrokerageCommissionAndPayment.RemoveRange(existingBrokerageCommissions);

                // Add new
                foreach (var commission in model.BrokerageCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var brokerageCommission = new CE_BrokerageCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,
                        StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_BrokerageCommissionAndPayment.Add(brokerageCommission);
                }
            }

            // Update Closer Commissions - Only for Closer sector type
            if (model.SectorType == "Closer" && model.CloserCommissions != null && model.CloserCommissions.Any())
            {
                var existingCloserCommissions = await db.CE_CloserCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();
                db.CE_CloserCommissionAndPayment.RemoveRange(existingCloserCommissions);

                foreach (var commission in model.CloserCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var closerCommission = new CE_CloserCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,
                        StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_CloserCommissionAndPayment.Add(closerCommission);
                }
            }

            // Update Introducer Commissions - Only for Introducer sector type
            if (model.SectorType == "Introducer" && model.IntroducerCommissions != null && model.IntroducerCommissions.Any())
            {
                var existingIntroducerCommissions = await db.CE_IntroducerCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();
                db.CE_IntroducerCommissionAndPayment.RemoveRange(existingIntroducerCommissions);

                foreach (var commission in model.IntroducerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var introducerCommission = new CE_IntroducerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        CommissionPercent = commission.Commission,
                        StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                        EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_IntroducerCommissionAndPayment.Add(introducerCommission);
                }
            }

            // Update Referral Partner Commissions - Only for Referral Partner sector type
            if (model.SectorType == "Referral Partner" && model.ReferralPartnerCommissions != null && model.ReferralPartnerCommissions.Any())
            {
                var existingReferralPartnerCommissions = await db.CE_ReferralPartnerCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();
                db.CE_ReferralPartnerCommissionAndPayment.RemoveRange(existingReferralPartnerCommissions);

                foreach (var commission in model.ReferralPartnerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
                {
                    var referralPartnerCommission = new CE_ReferralPartnerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        ReferralPartnerCommission = commission.ReferralPartnerCommission,
                        ReferralPartnerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                        ReferralPartnerEndDate = !string.IsNullOrEmpty(commission.ReferralPartnerEndDate) ? DateTime.Parse(commission.ReferralPartnerEndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_ReferralPartnerCommissionAndPayment.Add(referralPartnerCommission);
                }
            }

            // Update Lead Generator Commissions - Only for Leads Generator sector type
            if (model.SectorType == "Leads Generator" && model.LeadGeneratorCommissions != null && model.LeadGeneratorCommissions.Any())
            {
                var existingLeadGeneratorCommissions = await db.CE_LeadGeneratorCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();
                db.CE_LeadGeneratorCommissionAndPayment.RemoveRange(existingLeadGeneratorCommissions);

                foreach (var commission in model.LeadGeneratorCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
                {
                    var leadGeneratorCommission = new CE_LeadGeneratorCommissionAndPayment
                    {
                        SectorID = sectorId,
                        LeadGeneratorCommissionPercent = commission.LeadGeneratorCommission,
                        LeadGeneratorStartDate = !string.IsNullOrEmpty(commission.LeadGeneratorStartDate) ? DateTime.Parse(commission.LeadGeneratorStartDate) : (DateTime?)null,
                        LeadGeneratorEndDate = !string.IsNullOrEmpty(commission.LeadGeneratorEndDate) ? DateTime.Parse(commission.LeadGeneratorEndDate) : (DateTime?)null,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_LeadGeneratorCommissionAndPayment.Add(leadGeneratorCommission);
                }
            }
        }

        private async Task UpdateStaffAndSubsections(int sectorId, EditSectorViewModel model)
        {
            // Update Brokerage Staff
            if (model.BrokerageStaff != null && model.BrokerageStaff.Any())
            {
                var existingBrokerageStaff = await db.CE_BrokerageStaff
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();
                db.CE_BrokerageStaff.RemoveRange(existingBrokerageStaff);

                foreach (var staff in model.BrokerageStaff.Where(s => s != null && !string.IsNullOrEmpty(s.BrokerageStaffName)))
                {
                    var brokerageStaff = new CE_BrokerageStaff
                    {
                        SectorID = sectorId,
                        BrokerageStaffName = staff.BrokerageStaffName,
                        Email = staff.Email ?? "",
                        Mobile = staff.Mobile ?? "",
                        Landline = staff.Landline ?? "",
                        StartDate = !string.IsNullOrEmpty(staff.StartDate) ? DateTime.Parse(staff.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(staff.EndDate) ? DateTime.Parse(staff.EndDate) : (DateTime?)null,
                        Active = staff.Active
                    };
                    db.CE_BrokerageStaff.Add(brokerageStaff);
                }
            }

            // Update Sub Brokerages
            if (model.SubBrokerages != null && model.SubBrokerages.Any())
            {
                var existingSubBrokerages = await db.CE_SubBrokerage
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();
                db.CE_SubBrokerage.RemoveRange(existingSubBrokerages);

                foreach (var subBrokerage in model.SubBrokerages.Where(s => s != null && !string.IsNullOrEmpty(s.SubBrokerageName)))
                {
                    var subBrokerageEntity = new CE_SubBrokerage
                    {
                        SectorID = sectorId,
                        SubBrokerageName = subBrokerage.SubBrokerageName,
                        OfgemID = subBrokerage.OfgemID ?? "",
                        Active = subBrokerage.Active,
                        StartDate = !string.IsNullOrEmpty(subBrokerage.StartDate) ? DateTime.Parse(subBrokerage.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subBrokerage.EndDate) ? DateTime.Parse(subBrokerage.EndDate) : (DateTime?)null,
                        Email = subBrokerage.Email ?? "",
                        Landline = subBrokerage.Landline ?? "",
                        Mobile = subBrokerage.Mobile ?? ""
                    };
                    db.CE_SubBrokerage.Add(subBrokerageEntity);
                }
            }

            // Update Sub Referrals
            if (model.SubReferrals != null && model.SubReferrals.Any())
            {
                var existingSubReferrals = await db.CE_SubReferral
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();
                db.CE_SubReferral.RemoveRange(existingSubReferrals);

                foreach (var subReferral in model.SubReferrals.Where(s => s != null && !string.IsNullOrEmpty(s.SubReferralPartnerName)))
                {
                    var subReferralEntity = new CE_SubReferral
                    {
                        SectorID = sectorId,
                        SubReferralPartnerName = subReferral.SubReferralPartnerName,
                        Active = subReferral.Active,
                        StartDate = !string.IsNullOrEmpty(subReferral.StartDate) ? DateTime.Parse(subReferral.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subReferral.EndDate) ? DateTime.Parse(subReferral.EndDate) : (DateTime?)null,
                        SubReferralPartnerEmail = subReferral.Email ?? "",
                        SubReferralPartnerLandline = subReferral.Landline ?? "",
                        SubReferralPartnerMobile = subReferral.Mobile ?? ""
                    };
                    db.CE_SubReferral.Add(subReferralEntity);
                }
            }

            // Update Sub Introducers
            if (model.SubIntroducers != null && model.SubIntroducers.Any())
            {
                var existingSubIntroducers = await db.CE_SubIntroducer
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();
                db.CE_SubIntroducer.RemoveRange(existingSubIntroducers);

                foreach (var subIntroducer in model.SubIntroducers.Where(s => s != null && !string.IsNullOrEmpty(s.SubIntroducerName)))
                {
                    var subIntroducerEntity = new CE_SubIntroducer
                    {
                        SectorID = sectorId,
                        SubIntroducerName = subIntroducer.SubIntroducerName,
                        OfgemID = subIntroducer.OfgemID ?? "",
                        Active = subIntroducer.Active,
                        StartDate = !string.IsNullOrEmpty(subIntroducer.StartDate) ? DateTime.Parse(subIntroducer.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(subIntroducer.EndDate) ? DateTime.Parse(subIntroducer.EndDate) : (DateTime?)null,
                        SubIntroducerEmail = subIntroducer.Email ?? "",
                        SubIntroducerLandline = subIntroducer.Landline ?? "",
                        SubIntroducerMobile = subIntroducer.Mobile ?? ""
                    };
                    db.CE_SubIntroducer.Add(subIntroducerEntity);
                }
            }
        }

        #endregion
    }
}
