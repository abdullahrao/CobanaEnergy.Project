using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
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
        public DbSet<CE_ElectricContractLogs> CE_ElectricContractLogs { get; set; }
        public DbSet<CE_GasContracts> CE_GasContracts { get; set; }
        public DbSet<CE_GasContractLogs> CE_GasContractLogs { get; set; }
        public DbSet<CE_Accounts> CE_Accounts { get; set; }

    }
}