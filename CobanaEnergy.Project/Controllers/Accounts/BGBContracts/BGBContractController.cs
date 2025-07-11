using CobanaEnergy.Project.Controllers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.BGBContracts
{
    public class BGBContractController : BaseController
    {
        [HttpGet]
        public ActionResult EditBGBContract(string id)
        {
            return View("~/Views/Accounts/BGBContract/EditBGBContract.cshtml");
        }
    }
}