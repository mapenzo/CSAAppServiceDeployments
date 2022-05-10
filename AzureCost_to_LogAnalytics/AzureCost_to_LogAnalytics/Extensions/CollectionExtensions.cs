using Newtonsoft.Json;
using System.Collections.Generic;

namespace AzureCost_to_LogAnalytics.Extensions
{
    internal static class CollectionExtensions
    {
        public static string ToJson(this IEnumerable<CostDataLogEntry> logs)
        {
           return JsonConvert.SerializeObject(logs, Formatting.Indented);
        }
    }
}
