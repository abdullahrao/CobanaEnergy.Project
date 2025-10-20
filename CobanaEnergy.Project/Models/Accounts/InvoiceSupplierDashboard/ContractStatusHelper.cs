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

        public static readonly Dictionary<string, string> AllStatuses = new Dictionary<string, string>
        {
            { "Pending", "#A6A6A6" },
            { "Processing_Present Month", "#FFFF00" },
            { "Processing_Future Months", "#FFD966" },
            { "Objection", "#FF0000" },
            { "Objection Closed", "#B4A7D6" },
            { "Reapplied", "#A2C4C9" },
            { "New Lives", "#93C47D" },
            { "Live", "#00FF00" },
            { "Renewal Window", "#E69138" },
            { "Renewal Window - Ag Lost", "#F6B26B" },
            { "Renewed", "#6AA84F" },
            { "Possible Loss", "#00FFFF" },
            { "Lost", "#0B5394" },
            { "Credit Failed", "#999999" },
            { "Rejected", "#B7B7B7" },
            { "To Be Resolved - Cobana", "#FFD966" },
            { "Waiting Agent", "#00B0F0" },
            { "Waiting Supplier", "#9FC5E8" }
        };

    }
}