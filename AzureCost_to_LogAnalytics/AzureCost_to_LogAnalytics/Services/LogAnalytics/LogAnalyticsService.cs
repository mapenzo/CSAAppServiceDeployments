using AzureCost_to_LogAnalytics.Configuration;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics.Services
{
    public class LogAnalyticsService : ILogAnalyticsService
    {
        private string workspaceId;
        private string workspaceKey;
        private string logName;
        private string apiVersion;

        private readonly IAppSettingsService settingsService;

        private readonly bool isDev = App.Context.IsDevelopment();

        public LogAnalyticsService(IAppSettingsService settingsService)
        {
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        private void SetEnvironmentVariables()
        {
            workspaceId = isDev ? settingsService.GetValue<AppSettings>(e => e.WorkspaceId) : App.Context.GetVariable("workspaceid");
            workspaceKey = isDev ?  settingsService.GetValue<AppSettings>(e => e.WorkspaceKey) : App.Context.GetVariable("workspacekey");
            logName = isDev ? settingsService.GetValue<AppSettings>(e => e.LogName) : App.Context.GetVariable("logName");
            apiVersion = settingsService.GetValue("LogAnalyticsApiVersion");
        }

        public Task Post(string json)
        {
            SetEnvironmentVariables();

            string requestUriString = $"https://{workspaceId}.ods.opinsights.azure.com/api/logs?api-version={apiVersion}";

            DateTime dateTime = DateTime.UtcNow;

            string dateString = dateTime.ToString("r");

            string signature = GetSignature("POST", json.Length, "application/json", dateString, "/api/logs");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers["Log-Type"] = logName;
            request.Headers["x-ms-date"] = dateString;
            request.Headers["Authorization"] = signature;

            byte[] content = Encoding.UTF8.GetBytes(json);

            using (Stream requestStreamAsync = request.GetRequestStream())
            {
                requestStreamAsync.Write(content, 0, content.Length);
            }

            using HttpWebResponse responseAsync = (HttpWebResponse)request.GetResponse();
            if (responseAsync.StatusCode != HttpStatusCode.OK && responseAsync.StatusCode != HttpStatusCode.Accepted)
            {
                Stream responseStream = responseAsync.GetResponseStream();
                if (responseStream != null)
                {
                    using StreamReader streamReader = new StreamReader(responseStream);
                    throw new Exception(streamReader.ReadToEnd());
                }
            }

            return Task.CompletedTask;
        }

        private string GetSignature(string method, int contentLength, string contentType, string date, string resource)
        {
            string message = $"{method}\n{contentLength}\n{contentType}\nx-ms-date:{date}\n{resource}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            using HMACSHA256 encryptor = new(Convert.FromBase64String(workspaceKey));
            return $"SharedKey {workspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
        }
    }
}
