using AzureCost_to_LogAnalytics.Configuration;
using AzureCost_to_LogAnalytics.Services.Common;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace AzureCost_to_LogAnalytics.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IOptionsMonitor<AppSettings> optionsMonitor;
        private readonly IDictionaryConverter dictionaryConverter;

        public AppSettingsService(IOptionsMonitor<AppSettings> optionsMonitor, IDictionaryConverter dictionaryConverter)
        {
            this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            this.dictionaryConverter = dictionaryConverter ?? throw new ArgumentNullException(nameof(dictionaryConverter));
        }

        public string GetValue(string key)
        {
            var settings = 
                dictionaryConverter.Convert(optionsMonitor.CurrentValue);

            return settings.GetValueOrDefault(key);
        }
    }
}
