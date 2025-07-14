using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB
{
    public class EditBGBContractViewModel
    {
        public string Id { get; set; }
        public string SupplierId { get; set; }

        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; } 
        public string PaymentDate { get; set; }

        // Common
        public string Department { get; set; }
        public string BusinessName { get; set; }

        // Electric
        public string SalesTypeElectric { get; set; }
        public string MPAN { get; set; }
        public string DurationElectric { get; set; }
        public string InputDateElectric { get; set; }

        // Gas
        public string SalesTypeGas { get; set; }
        public string MPRN { get; set; }
        public string DurationGas { get; set; }
        public string InputDateGas { get; set; }

        public string ContractNotes { get; set; }

        public bool HasElectricDetails { get; set; } = true;
        public bool HasGasDetails { get; set; } = true;
    }
}