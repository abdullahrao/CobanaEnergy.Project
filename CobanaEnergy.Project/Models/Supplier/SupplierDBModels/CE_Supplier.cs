using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels
{
    [Table("CE_Supplier")]
    public class CE_Supplier
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
        public string CreatedAt { get; set; }
        public virtual ICollection<CE_SupplierProducts> CE_SupplierProducts { get; set; }
        public virtual ICollection<CE_SupplierContacts> CE_SupplierContacts { get; set; }

    }
}