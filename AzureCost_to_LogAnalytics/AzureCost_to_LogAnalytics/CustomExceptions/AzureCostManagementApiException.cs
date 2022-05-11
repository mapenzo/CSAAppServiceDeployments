using System;
using System.Runtime.Serialization;

namespace AzureCost_to_LogAnalytics
{
    public class AzureCostManagementApiException : Exception
    {
        private const string defaultMessage = "An error ocurred processing your request.";

        public AzureCostManagementApiException() : base(defaultMessage)
        {
        }

        public AzureCostManagementApiException(string message) : base(message)
        {
        }

        public AzureCostManagementApiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AzureCostManagementApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
