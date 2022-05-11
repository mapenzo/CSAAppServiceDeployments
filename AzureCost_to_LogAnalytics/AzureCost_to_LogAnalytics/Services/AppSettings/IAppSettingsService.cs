using System;
using System.Linq.Expressions;

namespace AzureCost_to_LogAnalytics.Services
{
    public interface IAppSettingsService
    {
        string GetValue(string key);
        string GetValue<T>(Expression<Func<T, object>> selector);
    }
}
