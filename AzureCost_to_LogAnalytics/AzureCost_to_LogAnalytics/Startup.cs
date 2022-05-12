using AzureCost_to_LogAnalytics;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using AzureCost_to_LogAnalytics.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using AzureCost_to_LogAnalytics.Services;
using AzureCost_to_LogAnalytics.Services.Common;

[assembly: FunctionsStartup(typeof(Startup))]
namespace AzureCost_to_LogAnalytics
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;

            builder.Services.AddOptions();
            builder.Services.Configure<AppSettings>(config.GetSection(nameof(AppSettings)));

            builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
            builder.Services.AddSingleton<IDictionaryConverter, DictionaryConverter>();
            builder.Services.AddSingleton<ILogAnalyticsService, LogAnalyticsService>();
            builder.Services.AddSingleton<IAzureManagmentService, AzureManagmentService>();

            builder.Services.AddLogging();
            
            builder.Services.AddHttpClient(Constants.HttpClientName, httpClient =>
            {
                if (!httpClient.DefaultRequestHeaders.Accept.Any(m => m.MediaType == Constants.JsonMediaType))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.JsonMediaType) {  CharSet = Encoding.UTF8.WebName });
                }
                httpClient.BaseAddress = new System.Uri("https://management.azure.com");
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            
            builder
                .ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            base.ConfigureAppConfiguration(builder);
        }        
    }
}
