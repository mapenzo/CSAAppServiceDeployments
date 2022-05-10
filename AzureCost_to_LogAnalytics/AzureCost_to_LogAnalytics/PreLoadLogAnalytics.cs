using AzureCost_to_LogAnalytics.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics
{
    public static class PreLoadLogAnalytics
    {
        private const string DefaultScope = "subscriptions/0d6c1317-f328-4b12-b90c-bb478eb29e74";

        private static string[] scopes;
        private static readonly string workspaceid = Environment.GetEnvironmentVariable("workspaceid") ?? "705378a9-ccb6-4dc4-b314-5f932376f1ee";
        private static readonly string workspacekey = Environment.GetEnvironmentVariable("workspacekey") ?? "SREVgO+OzvMO66C5zuLG4UXtiKxQNtqecR9ixrAUsgnuTpGORNK/u+J4nWKHq6T8cDgXktyble08mmz1PdwMvw==";
        private static readonly string logName = Environment.GetEnvironmentVariable("logName") ?? "AzureCostAnamolies";

        public static string JsonResult { get; set; }

        public static async Task CallAPIPage(string scope, string skipToken, string workspaceid, string workspacekey, string logName, ILogger log, string myJson)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string AuthToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            using var client = new HttpClient();
            // Setting Authorization.  
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);

            // Setting Base address.  
            client.BaseAddress = new Uri("https://management.azure.com");

            // Setting content type.  
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            AzureLogAnalytics logAnalytics = new AzureLogAnalytics(
                workspaceId: $"{workspaceid}",
                sharedKey: $"{workspacekey}",
                logType: $"{logName}");

            string newURL = "/" + scope + "/providers/Microsoft.CostManagement/query?api-version=2019-11-01&" + skipToken;
            var response = await client.PostAsync(newURL, new StringContent(myJson, Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                log.LogError(content, "An error ocurred processing your request.");
                return;
            }

            QueryResults result = JsonConvert.DeserializeObject<QueryResults>(content);

            if (result.properties.rows != null &&
                result.properties != null ||
                (result.properties.rows == null || !result.properties.rows.Any()))
            {
                log.LogError("There is no data to show.");
                return;
            }

            JsonResult = "[";
            for (int i = 0; i < result.properties.rows.Length; i++)
            {
                object[] row = result.properties.rows[i];
                double cost = Convert.ToDouble(row[0]);

                if (i == 0)
                {
                    JsonResult += $"{{\"PreTaxCost\": {cost},\"Date\": \"{row[1]}\",\"ResourceId\": \"{row[2]}\",\"ResourceType\": \"{row[3]}\",\"SubscriptionName\": \"{row[4]}\",\"ResourceGroup\": \"{row[5]}\"}}";
                }
                else
                {
                    JsonResult += $",{{\"PreTaxCost\": {cost},\"Date\": \"{row[1]}\",\"ResourceId\": \"{row[2]}\",\"ResourceType\": \"{row[3]}\",\"SubscriptionName\": \"{row[4]}\",\"ResourceGroup\": \"{row[5]}\"}}";
                }
            }

            JsonResult += "]";

            logAnalytics.Post(JsonResult);

            string nextLink = null;
            nextLink = result.properties.nextLink.ToString();

            if (!string.IsNullOrEmpty(nextLink))
            {
                skipToken = nextLink.Split('&')[1];
                Console.WriteLine(skipToken);
                await CallAPIPage(scope, skipToken, workspaceid, workspacekey, logName, log, myJson);
            }

        }


        [FunctionName("PreLoadLogAnalytics")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            scopes = GetScopes();

            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string AuthToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

                log.LogInformation("token: {token}", AuthToken);

                Console.WriteLine(AuthToken);

                using var client = new HttpClient();
                // Setting Authorization.  
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);

                // Setting Base address.  
                client.BaseAddress = new Uri("https://management.azure.com");

                // Setting content type.  
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                DateTime startTime = DateTime.UtcNow.AddDays(-30);
                DateTime endTime = DateTime.UtcNow;

                string start = startTime.ToString("MM/dd/yyyy");
                string end = endTime.ToString("MM/dd/yyyy");

                string myJson = @"{
                        'dataset': {
                            'aggregation': {
                            'totalCost': {
                                'function': 'Sum',
                                'name': 'PreTaxCost'
                            }
                        },
                        'granularity': 'Daily',
                        'grouping': [
                            {
                                'name': 'ResourceId',
                                'type': 'Dimension'
                            },
                            {
                                'name': 'ResourceType',
                                'type': 'dimension'
                            },
                            {
                                'name': 'SubscriptionName',
                                'type': 'dimension'
                            },
                            {
                                'name': 'ResourceGroup',
                                'type': 'dimension'
                            }
                        ]
                    },
                    'timePeriod': {
                        'from': '" + start + @"',
                        'to': '" + end + @"'
                    },
                    'timeframe': 'Custom',
                    'type': 'Usage'
                }";

                log.LogInformation($"Cost Query: {myJson}");

                log.LogInformation("WorkspaceId: {ws}", workspaceid);
                log.LogInformation("WorkspaceKey: {wsk}", workspacekey);
                log.LogInformation("Logname: {lg}", logName);

                AzureLogAnalytics logAnalytics = new(
                    workspaceId: $"{workspaceid}",
                    sharedKey: $"{workspacekey}",
                    logType: $"{logName}");

                foreach (string scope in scopes)
                {
                    log.LogInformation($"Scope: {scope}");

                    // HTTP Post
                    string endpoint = string.Concat("/", scope.Trim(), "/providers/Microsoft.CostManagement/query?api-version=2019-11-01");

                    log.LogInformation("endpoint -> {endpoint}", endpoint);

                    var response = await client.PostAsync(endpoint, new StringContent(myJson, Encoding.UTF8, "application/json"));

                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(content);
                    }

                    QueryResults result = JsonConvert.DeserializeObject<QueryResults>(content);

                    JsonResult = LogEntryMapper.Map(result.properties.rows).ToJson();
                    
                    logAnalytics.Post(JsonResult);

                    string nextLink = result.properties.nextLink?.ToString();

                    if (!string.IsNullOrEmpty(nextLink))
                    {
                        string skipToken = nextLink.Split('&')[1];
                        await CallAPIPage(scope, skipToken, workspaceid, workspacekey, logName, log, myJson);
                    }

                    JsonResult = "[";
                    for (int i = 0; i < result.properties.rows.Length; i++)
                    {
                        object[] row;
                        try
                        {
                            row = result.properties.rows[i];
                            double cost = row[0].AsDouble();
                            string sDate = row[1].AsString();
                            string sResourceId = row[2].AsString();
                            string sResourceType = row[3].AsString();
                            string sSubscriptionName = row[4].AsString();
                            string sResourceGroup = row[5].AsString();

                            if (i == 0)
                            {
                                JsonResult += $"{{\"PreTaxCost\": {cost},\"Date\": \"{sDate}\",\"ResourceId\": \"{sResourceId}\",\"ResourceType\": \"{sResourceType}\",\"SubscriptionName\": \"{sSubscriptionName}\",\"ResourceGroup\": \"{sResourceGroup}\"}}";
                            }
                            else
                            {
                                JsonResult += $",{{\"PreTaxCost\": {cost},\"Date\": \"{sDate}\",\"ResourceId\": \"{sResourceId}\",\"ResourceType\": \"{sResourceType}\",\"SubscriptionName\": \"{sSubscriptionName}\",\"ResourceGroup\": \"{sResourceGroup}\"}}";
                            }

                            JsonResult += "]";

                            log.LogInformation($"Cost Data: {JsonResult}");

                            logAnalytics.Post(JsonResult);

                            string continuationToken = result.properties.nextLink?.ToString();

                            if (!string.IsNullOrEmpty(continuationToken))
                            {
                                string skipToken = continuationToken.Split('&')[1];
                                await CallAPIPage(scope, skipToken, workspaceid, workspacekey, logName, log, myJson);
                            }

                            //return new OkObjectResult(jsonResult);
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, "An error ocurred processing your request.");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(ex.ToString());
                            Console.ResetColor();
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

        private static string[] GetScopes()
        {
            string scopes = Environment.GetEnvironmentVariable("scope");
            if (scopes != null && scopes.Contains(',', StringComparison.InvariantCultureIgnoreCase))
            {
                return scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                var scope = Environment.GetEnvironmentVariable("scope");
                if (!string.IsNullOrEmpty(scope))
                {
                    return new string[] { scope };
                }
                return new string[] { DefaultScope };
            }
        }
    }
}
