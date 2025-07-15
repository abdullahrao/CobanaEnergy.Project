using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

        public bool HasElectricDetails { get; set; } = false;
        public bool HasGasDetails { get; set; } = false;

        public string UpliftElectric { get; set; }
        public List<SelectListItem> ProductElectricList { get; set; } = new List<SelectListItem>();
        public long? SelectedProductElectric { get; set; }
        public string SupplierCommsTypeElectric { get; set; }
        public string CommissionElectric { get; set; }

        public List<SelectListItem> ProductGasList { get; set; } = new List<SelectListItem>();
        public long? SelectedProductGas { get; set; }
        public string SupplierCommsTypeGas { get; set; }
        public string CommissionGas { get; set; }
        public string UpliftGas { get; set; }

        public string InitialStartDate { get; set; }
        public string CED { get; set; }
    }
}