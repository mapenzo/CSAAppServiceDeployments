using System;
using System.Runtime.Serialization;

namespace AzureCost_to_LogAnalytics
{
    public class AzureLogAnalyticsApiException : Exception
    {
        private const string defaultMessage = "Azure Log Analytics threw an exception pro.";

        public AzureLogAnalyticsApiException() : base(defaultMessage)
        {
        }

        public AzureLogAnalyticsApiException(string message) : base(message)
        {
        }

        public AzureLogAnalyticsApiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AzureLogAnalyticsApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }   
}
