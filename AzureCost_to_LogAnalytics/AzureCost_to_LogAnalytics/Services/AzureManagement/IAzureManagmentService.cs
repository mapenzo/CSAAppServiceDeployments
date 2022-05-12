using System;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics.Services
{
    public interface IAzureManagmentService
    {
        Task<QueryResults> GetCostsAsync(double fromDays, string scope, string apiVersion, string skipToken = null);
    }
}
