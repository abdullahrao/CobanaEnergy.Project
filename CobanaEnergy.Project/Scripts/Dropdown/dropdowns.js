
const DropdownOptions = {
    department: ["In House", "Brokers", "Introductors"],
    source: ["Data", "Introducer", "Self-Gen", "Cobana RNW"],
    salesType: ["Renewal", "Acquisition", "COT"],
    salesTypeStatus: [
        "COT Awaiting Customer Self", "COT Awaiting Documents", "COT Awaiting Supplier",
        "COT Completed", "COT Customer Self Completed", "COT Not Completed", "MPAN/MPR Registration",
        "New Connection", "Termination Accepted", "Termination Rejected", "Termination Required",
        "Termination Sent", "N/A"
    ],
    supplierCommsType: ["Annual", "Residual", "Duration", "Quarterly"],
    preSalesStatus: [
        "Awaiting Audit Call", "Awaiting Details", "Awaiting LOA & CCF", "Awaiting Sign Contract",
        "Awaiting Supplier (Portal Issue)", "Contracts To Be Checked", "Duplicate Contract",
        "Failed Audit Call", "Failed Credit Check", "Incorrect Prices", "Incorrect Rejected Recording",
        "Meter not Supported (De-Energised)", "Meter Registration (MPAN/MPRN)",
        "Meter Registration Failed Credit", "Meter Registration Submitted",
        "New Connection (Site)", "New Connection Failed Credit", "New Connection Submitted",
        "Overturned Contract", "Ready to Submit", "Rejected Contract", "Submitted"
    ]
};

const AccountDropdownOptions = {
    contractStatus: [
        "Pending",
        "Processing_Present Month",
        "Processing_Future Months",
        "Objection",
        "Objection Closed",
        "Reapplied - Awaiting Confirmation",
        "New Lives",
        "Live",
        "Renewal Window",
        "Renewal Window - Ag Lost",
        "Renewed",
        "Contract Ended - Ag Lost",
        "Contract Ended - Not Renewed",
        "Contract Ended - Renewed",
        "Possible Loss",
        "Lost",
        "Credit Failed",
        "Rejected",
        "To Be Resolved - Internally",
        "Waiting Agent Response",
        "Waiting Supplier Response",
        "Dead - No Action Required",
        "Dead - Credit Failed",
        "Dead - Valid Contract in Place",
        "Dead - Duplicate Submission",
        "Dead - Due to Objections"
    ],
    paymentStatus: [
        "Advanced Payment",
        "Awaiting 1st Reconciliation",
        "Awaiting 2nd Initial",
        "Awaiting 2nd Reconciliation",
        "Awaiting 3rd Initial",
        "Awaiting 3rd Reconciliation",
        "Awaiting 4th Initial",
        "Awaiting 4th Reconciliation",
        "Awaiting 5th Initial",
        "Awaiting D19",
        "Awaiting Final Reconciliation",
        "Awaiting Invoice",
        "Awaiting Monthly Payment",
        "Clawback - COT Reconciliation",
        "Clawback - D19 Reconciliation",
        "Clawback - Duplicate Payments",
        "Clawback - Final Reconciliation",
        "Clawback - Never Live",
        "Commission Hold - Unpaid Bill",
        "Commission Hold - Unpaid Bill Clawback",
        "Contract Pending",
        "Discrepancies",
        "In Dispute",
        "Lost Confirmation",
        "Never Live - Resolved",
        "Resolve",
        "Reverse Clawback"
    ]
};