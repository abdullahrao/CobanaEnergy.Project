using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Web;

namespace CobanaEnergy.Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Optional: Extend user profile
        // public string FullName { get; set; }
    }
}