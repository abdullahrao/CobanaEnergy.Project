﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Dual.EditDual
{
    public class DualContractEditViewModel
    {
        public string EId { get; set; }
        [Required]
        public string Department { get; set; }
        [Required]
        public string Agent { get; set; }
        [Required]
        public string Source { get; set; }
        [Required]
        public string Introducer { get; set; }
        [Required]
        public string SubIntroducer { get; set; }
        [Required]
        public string ElectricSalesType { get; set; }
        [Required]
        public string GasSalesType { get; set; }
        [Required]
        public string ElectricSalesTypeStatus { get; set; }
        [Required]
        public string GasSalesTypeStatus { get; set; }

        [Required(ErrorMessage = "Top Line is required.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Top Line must be exactly 8 characters.")]
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Top Line must be alphanumeric.")]
        public string TopLine { get; set; }

        [Required(ErrorMessage = "MPAN is required.")]
        [RegularExpression(@"^\d{13}$|^N/A$", ErrorMessage = "MPAN must be 13 digits or 'N/A'.")]
        public string MPAN { get; set; }

        [Required(ErrorMessage = "MPRN is required.")]
        [RegularExpression(@"^\d{6,10}$|^N/A$", ErrorMessage = "MPRN must be 6–10 digits or 'N/A'.")]
        public string MPRN { get; set; }

        [Required]
        public string ElectricCurrentSupplier { get; set; }
        [Required]
        public string GasCurrentSupplier { get; set; }
        [Required]
        public long ElectricSupplierId { get; set; }
        [Required]
        public long ElectricProductId { get; set; }
        [Required]
        public long GasSupplierId { get; set; }
        [Required]
        public long GasProductId { get; set; }
        [Required]
        public string ElectricSupplierCommsType { get; set; }
        [Required]
        public string GasSupplierCommsType { get; set; }
        [Required]
        public string BusinessName { get; set; }
        [Required]
        public string CustomerName { get; set; }
        [Required]
        public string BusinessDoorNumber { get; set; }
        [Required]
        public string BusinessHouseName { get; set; }
        [Required]
        public string BusinessStreet { get; set; }
        [Required]
        public string BusinessTown { get; set; }
        [Required]
        public string BusinessCounty { get; set; }
        [Required]
        public string PostCode { get; set; }

        [Required]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Must be 11 digits.")]
        public string PhoneNumber1 { get; set; }

        [Required]
        [RegularExpression(@"^\d{11}$|^N/A$", ErrorMessage = "Enter 11 digits or 'N/A'.")]
        public string PhoneNumber2 { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }
        public string ElectricInputDate { get; set; }
        public string GasInputDate { get; set; }
        [Required]
        public string ElectricInitialStartDate { get; set; }
        [Required]
        public string GasInitialStartDate { get; set; }

        [Required(ErrorMessage = "Electric Duration required.")]
        [Range(1, 10, ErrorMessage = "Electric Duration must be 1–10 years.")]
        public string ElectricDuration { get; set; }

        [Required(ErrorMessage = "Gas Duration required.")]
        [Range(1, 10, ErrorMessage = "Gas Duration must be 1–10 years.")]
        public string GasDuration { get; set; }

        [Required(ErrorMessage = "Electric Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Uplift must be a valid decimal between 0.00 and 100")]
        public decimal ElectricUplift { get; set; }

        [Required(ErrorMessage = "Electric InputEAC is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal ElectricInputEAC { get; set; }

        [Required(ErrorMessage = "Electric Day Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Day Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricDayRate { get; set; }

        [Required(ErrorMessage = "Electric Night Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Night Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricNightRate { get; set; }

        [Required(ErrorMessage = "Electric Evening/Weekend Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Evening/Weekend Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricEveWeekendRate { get; set; }

        [Required(ErrorMessage = "Electric Other Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Other Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricOtherRate { get; set; }

        [Required(ErrorMessage = "Electric Standing Charge is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Standing Charge must be a valid decimal between 0.00 and 100")]
        public decimal ElectricStandingCharge { get; set; }

        [Required(ErrorMessage = "Gas Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Uplift must be a valid decimal between 0.00 and 100")]
        public decimal GasUplift { get; set; }

        [Required(ErrorMessage = "Gas InputEAC is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal GasInputEAC { get; set; }

        [Required(ErrorMessage = "Gas Unit Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Unit Rate must be a valid decimal between 0.00 and 100")]
        public decimal GasUnitRate { get; set; }

        [Required(ErrorMessage = "Gas Other Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Other Rate must be a valid decimal between 0.00 and 100")]
        public decimal GasOtherRate { get; set; }

        [Required(ErrorMessage = "Gas Standing Charge is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Standing Charge must be a valid decimal between 0.00 and 100")]
        public decimal GasStandingCharge { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Sort Code must be 6 digits.")]
        public string SortCode { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Account Number must be 8 digits.")]
        public string AccountNumber { get; set; }

        [Required]
        public string ElectricPreSalesStatus { get; set; }
        [Required]
        public string GasPreSalesStatus { get; set; }

        public string EMProcessor { get; set; }
        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }
        public string ContractNotes { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Type { get; set; } = "Dual";
    }
}