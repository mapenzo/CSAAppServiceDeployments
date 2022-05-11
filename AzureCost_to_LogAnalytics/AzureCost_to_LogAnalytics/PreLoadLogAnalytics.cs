using AzureCost_to_LogAnalytics.Configuration;
using AzureCost_to_LogAnalytics.Extensions;
using AzureCost_to_LogAnalytics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics
{
    public class PreLoadLogAnalytics
    {
        private string[] scopes;
        private string workspaceId;
        private string workspaceKey;
        private string logName;

        public static string JsonResult { get; internal set; }

        private readonly IAppSettingsService settingsService;
        private readonly ILogAnalyticsService logAnalyticsService;
        private readonly HttpClient client;

        private readonly bool isDev = App.Context.IsDevelopment();

        public PreLoadLogAnalytics(
            IAppSettingsService settingsService,           
            IHttpClientFactory httpClientFactory,
            ILogAnalyticsService logAnalyticsService)
        {
            this.settingsService = settingsService;
            this.logAnalyticsService = logAnalyticsService;

            SetEnvironmentVariables();

            client = httpClientFactory.CreateClient(Constants.HttpClientName);
        }

        private void SetEnvironmentVariables()
        {
            workspaceId = isDev ? settingsService.GetValue<AppSettings>(e => e.WorkspaceId) : App.Context.GetVariable("workspaceid");
            workspaceKey = isDev ? settingsService.GetValue<AppSettings>(e => e.WorkspaceKey) : App.Context.GetVariable("workspacekey");
            logName = isDev ? settingsService.GetValue<AppSettings>(e => e.LogName) : App.Context.GetVariable("logName");
            scopes = GetScopes(settingsService, isDev);
        }

        [FunctionName("PreLoadLogAnalytics")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            log.LogInformation("Environment: {env}", Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"));

            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string AuthToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);

                string costQueryJson = GetCostsQuery(settingsService);

                var apiVersion = settingsService.GetValue(nameof(AppSettings.CostManagementApiVersion));

                foreach (string scope in scopes)
                {
                    var result = await SaveCostsToLogAnalyticsAsync(costQueryJson, scope, apiVersion);

                    string nextLink = result.properties.nextLink?.ToString();

                    if (!string.IsNullOrEmpty(nextLink))
                    {
                        string skipToken = nextLink.Split('&')[1];
                        while (!string.IsNullOrWhiteSpace(skipToken))
                        {
                            result = await SaveCostsToLogAnalyticsAsync(costQueryJson, scope, apiVersion, skipToken);
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
                return new BadRequestObjectResult(ex.ToString());
            }

            return new OkObjectResult(JsonResult);
        }

        private async Task<QueryResults> SaveCostsToLogAnalyticsAsync(string costQueryJson, string scope, string apiVersion, string skipToken = null)
        {
            // HTTP Post
            string endpoint = GetAzureManagementEndpoint(scope, apiVersion, skipToken);

            var httpContent = new StringContent(costQueryJson, Encoding.UTF8, Constants.JsonMediaType);

            var response = await client.PostAsync(endpoint, httpContent);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureCostManagementApiException(content);
            }

            QueryResults result = JsonConvert.DeserializeObject<QueryResults>(content);

            JsonResult = LogEntryMapper.Map(result.properties.rows).ToJson();

            await logAnalyticsService.Post(JsonResult);

            return result;
        }

        private static string GetAzureManagementEndpoint(string scope, string apiVersion, string skipToken = null)
        {
            return string.IsNullOrWhiteSpace(skipToken)
                ? string.Concat("/", scope.Trim(), $"/providers/Microsoft.CostManagement/query?api-version={apiVersion}")
                : string.Concat("/", scope.Trim(), $"/providers/Microsoft.CostManagement/query?api-version={apiVersion}&{skipToken}");
        }

        private static string GetCostsQuery(IAppSettingsService settingsService)
        {
            var fromDays = settingsService.GetValue<CostQueryTimePeriod>(e => e.FromDaysAgo).AsDouble();

            DateTime startTime = DateTime.UtcNow.AddDays(fromDays);
            DateTime endTime = DateTime.UtcNow;

            string start = startTime.ToString("MM/dd/yyyy");
            string end = endTime.ToString("MM/dd/yyyy");

            return CostQueryBuilder.Build(start, end);
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
