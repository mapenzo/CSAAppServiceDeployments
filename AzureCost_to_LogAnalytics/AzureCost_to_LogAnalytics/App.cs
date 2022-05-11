using System;

namespace AzureCost_to_LogAnalytics
{
    internal static class App
    {
        public static class Context
        {
            public static bool IsDevelopment() 
                => Environment.GetEnvironmentVariable(Constants.AzureFunctionsEnvironment) == Constants.DevelopmentEnvironment;

            public static string GetVariable(string key)
                => Environment.GetEnvironmentVariable(key);
        }
    }
}
