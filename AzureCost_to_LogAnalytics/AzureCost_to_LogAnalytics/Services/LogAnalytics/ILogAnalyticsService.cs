using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics.Services
{
    public interface ILogAnalyticsService
    {
        Task Post(string json);
    }
}
