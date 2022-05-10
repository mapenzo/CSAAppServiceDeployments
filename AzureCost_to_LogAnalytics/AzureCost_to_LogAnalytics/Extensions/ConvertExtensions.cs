using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics.Extensions
{
    internal static class ConvertExtensions
    {
        public static string AsString(this object value)
        {
            try
            {
                return Convert.ToString(value);
            }
            catch 
            {
                return string.Empty;
            }
        }

        public static string AsDateString(this object value)
        {
            try
            {
                return Convert.ToString(value);
            }
            catch
            {
                return DateTimeOffset.UtcNow.Date.ToShortDateString();
            }
        }

        public static double AsDouble(this object value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0d;
            }
        }
    }
}
