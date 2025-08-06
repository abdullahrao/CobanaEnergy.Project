using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDBContext() : base("CobanaConnection") { }

        public static ApplicationDBContext Create()
        {
            return new ApplicationDBContext();
        }

        // Add other DbSets here (e.g., Suppliers, Contracts)
        // public DbSet<Supplier> Suppliers { get; set; }     
        public DbSet<CE_Supplier> CE_Supplier { get; set; }
        public DbSet<CE_SupplierContacts> CE_SupplierContacts { get; set; }
        public DbSet<CE_SupplierProducts> CE_SupplierProducts { get; set; }
        public DbSet<CE_ElectricContracts> CE_ElectricContracts { get; set; }
        public DbSet<CE_SupplierUplifts> CE_SupplierUplifts { get; set; }
        public DbSet<CE_ElectricContractLogs> CE_ElectricContractLogs { get; set; }
        public DbSet<CE_GasContracts> CE_GasContracts { get; set; }
        public DbSet<CE_GasContractLogs> CE_GasContractLogs { get; set; }
        public DbSet<CE_Accounts> CE_Accounts { get; set; }
        public DbSet<CE_ElectricSupplierSnapshots> CE_ElectricSupplierSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierContactSnapshots> CE_ElectricSupplierContactSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierProductSnapshots> CE_ElectricSupplierProductSnapshots { get; set; }
        public DbSet<CE_ElectricSupplierUpliftSnapshots> CE_ElectricSupplierUpliftSnapshots { get; set; }
        public DbSet<CE_GasSupplierSnapshots> CE_GasSupplierSnapshots { get; set; }
        public DbSet<CE_GasSupplierContactSnapshots> CE_GasSupplierContactSnapshots { get; set; }
        public DbSet<CE_GasSupplierProductSnapshots> CE_GasSupplierProductSnapshots { get; set; }
        public DbSet<CE_GasSupplierUpliftSnapshots> CE_GasSupplierUpliftSnapshots { get; set; }
        public DbSet<CE_InvoiceSupplierUploads> CE_InvoiceSupplierUploads { get; set; }
        public DbSet<CE_EacLogs> CE_EacLogs { get; set; }
        public DbSet<CE_ContractStatuses> CE_ContractStatuses { get; set; }
        public DbSet<CE_CommissionAndReconciliation> CE_CommissionAndReconciliation { get; set; }
        public DbSet<CE_CommissionMetrics> CE_CommissionMetrics { get; set; }
        public DbSet<CE_PaymentAndNoteLogs> CE_PaymentAndNoteLogs { get; set; }
    }
}