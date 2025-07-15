using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB
{
    public class UpdateContractViewModel
    {
        public string EId { get; set; }
        public bool HasElectricDetails { get; set; }
        public bool HasGasDetails { get; set; }

        public string UpliftElectric { get; set; }
        public string SupplierCommsTypeElectric { get; set; }
        public long ElectricSnapshotId { get; set; }

        public string UpliftGas { get; set; }
        public string SupplierCommsTypeGas { get; set; }
        public long GasSnapshotId { get; set; }

        public string ContractNotes { get; set; }
    }
}