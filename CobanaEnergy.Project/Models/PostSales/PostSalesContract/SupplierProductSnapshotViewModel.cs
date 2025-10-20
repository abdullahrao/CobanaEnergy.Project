using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.PostSales.PostSalesContract
{
    public class SupplierProductSnapshotViewModel
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string SupplierCommsType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Commission { get; set; }
    }


    public class SupplierSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string Link { get; set; }
        public List<SupplierProductSnapshotViewModel> Products { get; set; } = new List<SupplierProductSnapshotViewModel>();
        public List<SupplierContactSnapshotViewModel> Contacts { get; set; }
        public List<SupplierUpliftSnapshotViewModel> Uplifts { get; set; }
    }


    public class SupplierContactSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
    }


    public class SupplierUpliftSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PostSalesEditViewModel
    {
        [Required]
        public string EId { get; set; }
        // Removed: Agent, Introducer, SubIntroducer fields as per requirements

        [Required(ErrorMessage = "MPRN is required.")]
        [RegularExpression(@"^\d{6,10}$|^N/A$", ErrorMessage = "MPRN must be between 6-10 digits or 'N/A'.")]
        public string MPRN { get; set; }

        [Required]
        public string BusinessName { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string BusinessDoorNumber { get; set; }

        public string BusinessHouseName { get; set; }
        [Required]
        public string BusinessStreet { get; set; }
        [Required]
        public string BusinessTown { get; set; }

        public string BusinessCounty { get; set; }
        [Required]
        public string PostCode { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain only digits.")]
        public string PhoneNumber1 { get; set; }

        public string PhoneNumber2 { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        public string InputDate { get; set; }

        [Required]
        public string InitialStartDate { get; set; }

        [Required(ErrorMessage = "Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Uplift must be a valid decimal between 0.00 and 100")]
        public decimal Uplift { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 10, ErrorMessage = "Duration must be between 1 and 10 years.")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "InputEAC is required")]
        // [Range(0.00, 100.00, ErrorMessage = "InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal InputEAC { get; set; }

        [Required(ErrorMessage = "UnitRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "UnitRate must be a valid decimal between 0.00 and 100")]
        public decimal UnitRate { get; set; }

        [Required(ErrorMessage = "OtherRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "OtherRate must be a valid decimal between 0.00 and 100")]
        public decimal OtherRate { get; set; }

        [Required(ErrorMessage = "StandingCharge is required")]
        //[Range(0.00, 100.00, ErrorMessage = "StandingCharge must be a valid decimal between 0.00 and 100")]
        public decimal StandingCharge { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Sort Code must be exactly 6 digits.")]
        public string SortCode { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Account Number must be exactly 8 digits.")]
        public string AccountNumber { get; set; }

        [Required]
        public string CurrentSupplier { get; set; }
        [Required]
        public long SupplierId { get; set; }
        [Required]
        public long ProductId { get; set; }

        public string EMProcessor { get; set; }

        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }

        public string ContractNotes { get; set; }

        [Required]
        public string Department { get; set; }

        // New dynamic fields based on Department
        public int? CloserId { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }
        public int? SubBrokerageId { get; set; }
        public string Collaboration { get; set; }
        public int? LeadGeneratorId { get; set; }

        [Required]
        public string Source { get; set; }
        [Required]
        public string SalesType { get; set; }
        [Required]
        public string SalesTypeStatus { get; set; }
        [Required]
        public string SupplierCommsType { get; set; }
        [Required]
        public string PreSalesStatus { get; set; }

        public string PreSalesFollowUpDate { get; set; }

        public SupplierSnapshotViewModel SupplierSnapshot { get; set; }

        // Brokerage Details
        [Required(ErrorMessage = "Brokerage is required")]
        public int BrokerageId { get; set; }

        public string OfgemId { get; set; }
    }

}