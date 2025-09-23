using CobanaEnergy.Project.Models.Accounts.ProblematicsDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard
{

    public class AccountMasterDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<AccountMasterRowViewModel> AccountsContracts { get; set; } = new List<AccountMasterRowViewModel>();
    }


    public class AccountMasterRowViewModel
    {
        public string ContractId { get; set; }
        public string EId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string BusinessName { get; set; }
        public string MPXN { get; set; }
        public string InputEAC { get; set; } 
        public string SupplierEAC { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string PreviousInvoiceNumbers { get; set; } // comma separated
        public string AccountsNotes { get; set; }
        public string CommissionForecast { get; set; }
        public string CobanaDueCommission { get; set; }
        public decimal? CobanaPaidCommission { get; set; }
        public string ContractType { get; set; }
        public string CobanaFinalReconciliation { get; set; }
        /// <summary>
        /// Action and Controller 
        /// </summary>
        public string Controller { get; set; }
        public string Action { get; set; }

    }

    public class ContractViewModel
    {
        public string Id { get; set; }
        public string EId { get; set; }
        public string MPXN { get; set; }
        public string ContractType { get; set; }
        public string BusinessName { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string InputEAC { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Department { get; set; }
        public int? BrokerageId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? SubBrokerageId { get; set; }
        public int? CloserId { get; set; }
        public int? LeadGeneratorId { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }

        // Reconciliation
        public long? ReconciliationId { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string CobanaDueCommission { get; set; }
        public string CobanaFinalReconciliation { get; set; }

        // Metrics
        public string ContractDurationDays { get; set; }
        public string LiveDays { get; set; }
        public string PercentLiveDays { get; set; }
        public string TotalCommissionForecast { get; set; }
        public string InitialCommissionForecast { get; set; }
        public string TotalAverageEAC { get; set; }
    }
}