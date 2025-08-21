using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Common
{
    public static class SupportedSuppliers
    {
        public static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "british gas business",
                "bg lite",
                "corona"
            };

        public static readonly string[] MeterHeaders = { "meternum", "meterpoint", "mpancore" };

        /// <summary>
        /// Add more supplier here 
        /// </summary>
        public static readonly Dictionary<string, Tuple<string, string>> Map =
        new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "British Gas Business", Tuple.Create("BGBContract", "EditBGBContract") },
            { "BG Lite", Tuple.Create("BGLiteContract", "EditBGLiteContract") },
            { "Corona", Tuple.Create("CoronaContract", "EditCoronaContract") }
            // add more suppliers here later
        };

        public const string DefaultAction = "EditSingleContract";
        public const string DefaultController = "Contract";
    }
}