using AzureCost_to_LogAnalytics.Configuration;
using AzureCost_to_LogAnalytics.Services.Common;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AzureCost_to_LogAnalytics.Extensions;

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

        public string GetValue<T>(Expression<Func<T, object>> selector)
        {
            var settings =
                dictionaryConverter.Convert(optionsMonitor.CurrentValue);

            var property = selector.GetMemberName();
            var value = settings.GetValueOrDefault(property);
            if (string.IsNullOrEmpty(value))
            {
                property = typeof(T).Name + "." + property;
                value = settings.GetValueOrDefault(property);
            }
            return value;
        }
    }
}
