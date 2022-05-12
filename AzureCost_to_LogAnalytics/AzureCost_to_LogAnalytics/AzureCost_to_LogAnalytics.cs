using AzureCost_to_LogAnalytics.Configuration;
using AzureCost_to_LogAnalytics.Extensions;
using AzureCost_to_LogAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics
{
    public class AzureCost_to_LogAnalytics
    {
        private string[] scopes;

        private readonly IAzureManagmentService managmentService;
        private readonly IAppSettingsService settingsService;
        private readonly ILogAnalyticsService logAnalyticsService;

        private readonly bool isDev = App.Context.IsDevelopment();

        public AzureCost_to_LogAnalytics(
            IAzureManagmentService managmentService,
            IAppSettingsService settingsService,
            ILogAnalyticsService logAnalyticsService)
        {
            this.settingsService = settingsService;
            this.logAnalyticsService = logAnalyticsService;
            this.managmentService = managmentService;
        }

        [FunctionName("DailyCostLoad")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] TimerInfo myTimer, ILogger log)
        {
            TimeSpan start = (DateTime.UtcNow - DateTime.UtcNow.AddDays(-1));

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation("Environment: {env}", Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"));

            string jsonResult = string.Empty;

            try
            {
                scopes = GetScopes(settingsService, isDev);

                var fromDays = start.TotalDays;
                var apiVersion = settingsService.GetValue(nameof(AppSettings.CostManagementApiVersion));

                foreach (string scope in scopes)
                {
                    var result = await managmentService.GetCostsAsync(fromDays, scope, apiVersion);

                    jsonResult = LogEntryMapper.Map(result.properties.rows).ToJson();

                    await logAnalyticsService.Post(jsonResult);

                    string nextLink = result.properties.nextLink?.ToString();

                    if (!string.IsNullOrEmpty(nextLink))
                    {
                        string skipToken = nextLink.Split('&')[1];
                        while (!string.IsNullOrWhiteSpace(skipToken))
                        {
                            result = await managmentService.GetCostsAsync(fromDays, scope, apiVersion, skipToken);
                            jsonResult = LogEntryMapper.Map(result.properties.rows).ToJson();
                            await logAnalyticsService.Post(jsonResult);
                            nextLink = result.properties.nextLink?.ToString();
                            skipToken = !string.IsNullOrWhiteSpace(nextLink) ? nextLink.Split('&')[1] : null;
                            await Task.Delay(200);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error ocurred processing your request.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();                
            }
        }

        private static string[] GetScopes(IAppSettingsService settingsService, bool isDev)
        {
            string scopes = isDev ? settingsService.GetValue<AppSettings>(e => e.Scopes) : App.Context.GetVariable("scope");
            if (scopes == null)
            {
                throw new InvalidProgramException("Scope value is missing.");
            }

            if (scopes.Contains(',', StringComparison.InvariantCultureIgnoreCase))
            {
                return scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return new string[] { scopes };
            }
        }
    }
}
