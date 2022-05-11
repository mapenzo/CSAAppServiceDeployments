using System.Collections.Generic;

namespace AzureCost_to_LogAnalytics.Configuration
{
    public class AppSettings
    {
        public IEnumerable<string> Scopes { get; set; }
        public string WorkspaceId { get; set; }
        public string WorkspaceKey { get; set; }
        public string LogName { get; set; }
        public string CostManagementApiVersion { get; set; }
        public string LogAnalyticsApiVersion { get; set; }
        public CostQueryTimePeriod CostQueryTimePeriod { get; set; }
    }

    public class CostQueryTimePeriod
    {
        public double FromDaysAgo { get; set; }
    }
}
