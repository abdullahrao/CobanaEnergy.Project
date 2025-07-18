using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard
{
    public static class ContractStatusHelper
    {
        public static readonly HashSet<string> ExcludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Objection Closed|Never Live - Resolved",
            "Contract Ended - Ag Lost|Resolve",
            "Contract Ended - Not Renewed|Resolve",
            "Contract Ended - Renewed|Resolve",
            "Lost|Resolve",
            "Credit Failed|Never Live - Resolved",
            "Rejected|Never Live - Resolved",
            "Dead - No Action Required|Never Live - Resolved",
            "Dead - Credit Failed|Never Live - Resolved",
            "Dead - Valid Contract in Place|Never Live - Resolved",
            "Dead - Duplicate Submission|Never Live - Resolved",
            "Dead - Due to Objections|Never Live - Resolved"
        };

        public static bool IsExcluded(string contractStatus, string paymentStatus)
        {
            string key = $"{contractStatus ?? ""}|{paymentStatus ?? ""}";
            return ExcludedKeys.Contains(key);
        }
    }
}