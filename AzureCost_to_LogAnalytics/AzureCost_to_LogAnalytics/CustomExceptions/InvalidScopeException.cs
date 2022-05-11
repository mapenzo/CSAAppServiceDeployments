using System;
using System.Runtime.Serialization;

namespace AzureCost_to_LogAnalytics
{
    [Serializable]
    public class InvalidScopeException : Exception
    {
        private const string defaultMessage = "The provided scope isn't valid.";

        public InvalidScopeException() : base(defaultMessage)
        {
        }

        public InvalidScopeException(string message) : base(message)
        {
        }

        public InvalidScopeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidScopeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
