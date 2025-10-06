using System;
using System.Globalization;

namespace CobanaEnergy.Project.Helpers
{
    /// <summary>
    /// Helper class for parsing and formatting data
    /// </summary>
    public static class ParserHelper
    {
        /// <summary>
        /// Format date string from dd/MM/yyyy or yyyy-MM-dd to dd-MM-yy for display
        /// </summary>
        /// <param name="dateString">The date string to format</param>
        /// <returns>Formatted date string in dd-MM-yy format or "N/A" if invalid</returns>
        public static string FormatDateForDisplay(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString == "N/A" || dateString == "-")
                return "N/A";

            // Try dd/MM/yyyy format first (primary format)
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result.ToString("dd-MM-yy");

            // Try yyyy-MM-dd format (ISO format)
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result.ToString("dd-MM-yy");

            // Try dd-MM-yyyy format (already formatted)
            if (DateTime.TryParseExact(dateString, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result.ToString("dd-MM-yy");

            return dateString; // Return original if parsing fails
        }

        /// <summary>
        /// Parse date string in various formats to DateTime for sorting
        /// </summary>
        /// <param name="dateString">The date string to parse</param>
        /// <returns>Parsed DateTime or DateTime.MinValue if invalid</returns>
        public static DateTime ParseDateForSorting(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString == "N/A")
                return DateTime.MinValue;

            // Try dd/MM/yyyy format first (primary format)
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            // Try yyyy-MM-dd format (ISO format)
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            // Try dd-MM-yyyy format
            if (DateTime.TryParseExact(dateString, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }
    }
}
