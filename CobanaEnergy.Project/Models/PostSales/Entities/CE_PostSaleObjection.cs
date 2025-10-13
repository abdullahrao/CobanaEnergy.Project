using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.PostSales.Entities
{
    [Table("CE_PostSaleObjection")]
    public class CE_PostSaleObjection
    {
        public int Id { get; set; }
        public string ObjectionDate { get; set; }
        public string QueryType { get; set; }
        public string EId { get; set; }
        public string ContractType { get; set; }
        public int ObjectionCount { get; set; }
        public string ReAppliedDate { get; set; }
        public int ReAppliedCount { get; set; }
        public string ModifyBy { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifyDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}