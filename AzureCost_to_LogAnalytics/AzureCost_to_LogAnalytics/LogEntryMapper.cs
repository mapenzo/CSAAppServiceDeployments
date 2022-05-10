using AzureCost_to_LogAnalytics.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureCost_to_LogAnalytics
{
    internal class LogEntryMapper
    {
        public static IEnumerable<CostDataLogEntry> Map(object[][] rows)
        {
            var result = new List<CostDataLogEntry>();

            if (rows.Length == 0)
            {
                result.Add(ToEmptyCostData());
            }

            foreach (object[] row in rows)
            {
                double cost = row[0].AsDouble();
                string sDate = row[1].AsDateString();
                string sResourceId = row[2].AsString();
                string sResourceType = row[3].AsString();
                string sSubscriptionName = row[4].AsString();
                string sResourceGroup = row[5].AsString();

                var costData = new CostDataLogEntry
                {
                    Date = sDate,
                    PreTaxCost = cost,
                    ResourceGroup = sResourceGroup,
                    ResourceId = sResourceId,
                    ResourceType = sResourceType,
                    SubscriptionName = sSubscriptionName
                };

                result.Add(costData);
            }

            return result.AsEnumerable<CostDataLogEntry>();
        }

        private static CostDataLogEntry ToEmptyCostData()
        {
            double cost = 0.0d;
            string sDate = DateTimeOffset.UtcNow.DateTime.ToShortDateString();
            string sResourceId = "";
            string sResourceType = "";
            string sSubscriptionName = "Visual Studio Enterprise Subscription";
            string sResourceGroup = "";

            return new CostDataLogEntry
            {
                Date = sDate,
                PreTaxCost = cost,
                ResourceGroup = sResourceGroup,
                ResourceId = sResourceId,
                ResourceType = sResourceType,
                SubscriptionName = sSubscriptionName
            };
        }
    }
}
