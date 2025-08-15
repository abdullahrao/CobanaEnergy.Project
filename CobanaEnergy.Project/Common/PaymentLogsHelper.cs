using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Common
{
    public static class PaymentLogsHelper
    {
        public static void InsertPaymentAndNotesLogs(
        ApplicationDBContext db, PaymentAndNotesLogsViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PaymentStatus) || !string.IsNullOrEmpty(model.CobanaInvoiceNotes))
            {
                var log = new CE_PaymentAndNoteLogs
                {
                    EId = model.EId,
                    PaymentStatus = model.PaymentStatus,
                    CobanaInvoiceNotes = model.CobanaInvoiceNotes,
                    Username = model.Username ?? "Unknown User",
                    contracttype = model.ContractType,
                    Dashboard = model.Dashboard,
                    CreatedAt = DateTime.Now,
                };

                db.CE_PaymentAndNoteLogs.Add(log);
            }
        }
    }
}