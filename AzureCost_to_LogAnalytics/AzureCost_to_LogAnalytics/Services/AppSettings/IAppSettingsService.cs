namespace AzureCost_to_LogAnalytics.Services
{
    public interface IAppSettingsService
    {
        string GetValue(string key);
    }
}
