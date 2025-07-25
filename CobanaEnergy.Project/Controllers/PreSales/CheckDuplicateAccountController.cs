using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class CheckDuplicateAccountController : BaseController
    {
        private readonly ApplicationDBContext db;
        public CheckDuplicateAccountController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateAccountUnified(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return JsonResponse.Fail("Invalid account number.");

            try
            {
                var electricQuery = db.CE_Accounts
                    .Where(acc => acc.AccountNumber == account)
                    .Join(db.CE_ElectricContracts,
                        acc => acc.EId,
                        elec => elec.EId,
                        (acc, elec) => new
                        {
                            elec.EId,
                            acc.SortCode,
                            acc.AccountNumber,
                            Agent = elec.Agent,
                            BusinessName = elec.BusinessName,
                            CustomerName = elec.CustomerName,
                            InputDate = elec.InputDate,
                            PreSalesStatus = elec.PreSalesStatus,
                            Duration = elec.Duration,
                            acc.CreatedAt,
                            Type = "Electric"
                        });

                var gasQuery = db.CE_Accounts
                    .Where(acc => acc.AccountNumber == account)
                    .Join(db.CE_GasContracts,
                        acc => acc.EId,
                        gas => gas.EId,
                        (acc, gas) => new
                        {
                            gas.EId,
                            acc.SortCode,
                            acc.AccountNumber,
                            Agent = gas.Agent,
                            BusinessName = gas.BusinessName,
                            CustomerName = gas.CustomerName,
                            InputDate = gas.InputDate,
                            PreSalesStatus = gas.PreSalesStatus,
                            Duration = gas.Duration,
                            acc.CreatedAt,
                            Type = "Gas"
                        });

                var rawList = await electricQuery.Concat(gasQuery).ToListAsync();

                var merged = rawList
                    .GroupBy(x => x.EId)
                    .Select(g =>
                    {
                        var elec = g.FirstOrDefault(x => x.Type == "Electric" || x.Type == "Dual");
                        var gas = g.FirstOrDefault(x => x.Type == "Gas" || x.Type == "Dual");

                        bool isDual = elec != null && gas != null;

                        string mergedStatus = isDual
                            ? $"Electric: {elec?.PreSalesStatus}<br>Gas: {gas?.PreSalesStatus}"
                            : elec?.PreSalesStatus ?? gas?.PreSalesStatus ?? "-";

                        string mergedDate = isDual
                            ? $"Electric: {elec?.InputDate}<br>Gas: {gas?.InputDate}"
                            : elec?.InputDate ?? gas?.InputDate ?? "-";

                        DateTime.TryParse(elec?.InputDate, out var elecDate);
                        DateTime.TryParse(gas?.InputDate, out var gasDate);
                        DateTime sortable = elecDate > gasDate ? elecDate : gasDate;

                        return new
                        {
                            Agent = elec?.Agent ?? gas?.Agent ?? "-",
                            BusinessName = elec?.BusinessName ?? gas?.BusinessName ?? "-",
                            CustomerName = elec?.CustomerName ?? gas?.CustomerName ?? "-",
                            InputDate = mergedDate,
                            PreSalesStatus = mergedStatus,
                            Duration = elec?.Duration ?? gas?.Duration,
                            SortCode = elec?.SortCode ?? gas?.SortCode ?? "-",
                            AccountNumber = elec?.AccountNumber ?? gas?.AccountNumber ?? "-",
                            SortableDate = sortable
                        };
                    })
                    .OrderByDescending(x => x.SortableDate)
                    .ToList();

                return JsonResponse.Ok(merged);
            }
            catch (Exception ex)
            {
                Logger.Log("CheckDuplicateAccountUnified: " + ex);
                return JsonResponse.Fail("Error checking account number.");
            }
        }


    }
}