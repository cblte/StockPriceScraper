using System;
using System.Globalization;

namespace StockPriceScraper
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a string to a decimal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            return decimal.Parse(value.Replace(',', '.'), NumberStyles.Currency);
        }
    }
}
