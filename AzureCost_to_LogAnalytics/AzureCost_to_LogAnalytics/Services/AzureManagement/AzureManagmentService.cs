using AzureCost_to_LogAnalytics.Extensions;
using Microsoft.Azure.Services.AppAuthentication;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics.Services
{
    public class AzureManagmentService : IAzureManagmentService
    {
        private readonly HttpClient client;

        public AzureManagmentService(IHttpClientFactory httpClientFactory)
        {
            client = httpClientFactory.CreateClient(Constants.HttpClientName);
        }

        public async Task<QueryResults> GetCostsAsync(double fromDays, string scope, string apiVersion, string skipToken = null)
        {
            string token = await GetAuthToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var query = GetCostsQuery(fromDays);

            string endpoint = GetAzureManagementEndpoint(scope, apiVersion, skipToken);

            var httpContent = new StringContent(query, Encoding.UTF8, Constants.JsonMediaType);

            var response = await client.PostAsync(endpoint, httpContent);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureCostManagementApiException(content);
            }

            return JsonConvert.DeserializeObject<QueryResults>(content);
        }

        private static string GetAzureManagementEndpoint(string scope, string apiVersion, string skipToken = null)
        {
            return string.IsNullOrWhiteSpace(skipToken)
                ? string.Concat("/", scope.Trim(), $"/providers/Microsoft.CostManagement/query?api-version={apiVersion}")
                : string.Concat("/", scope.Trim(), $"/providers/Microsoft.CostManagement/query?api-version={apiVersion}&{skipToken}");
        }

        private static async Task<string> GetAuthToken()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");
        }

        private static string GetCostsQuery(double fromDays)
        {
            if (fromDays == 0)
            {
                fromDays = $"-{1}".AsDouble();
            }

            if (fromDays > 0)
            {
                fromDays = $"-{fromDays}".AsDouble();
            }

            DateTime startTime = DateTime.UtcNow.AddDays(fromDays);
            DateTime endTime = DateTime.UtcNow;

            string start = startTime.ToString("MM/dd/yyyy");
            string end = endTime.ToString("MM/dd/yyyy");

            return CostQueryBuilder.Build(start, end);
        }
    }
}
