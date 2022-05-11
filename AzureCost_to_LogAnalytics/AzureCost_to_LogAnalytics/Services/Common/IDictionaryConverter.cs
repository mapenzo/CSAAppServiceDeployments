using System.Collections.Generic;

namespace AzureCost_to_LogAnalytics.Services.Common
{
    public interface IDictionaryConverter
    {
        /// <summary>
        /// Converts an object into a <see cref="Dictionary{TKey, TValue}"/> by extracting all members and its values.
        /// </summary>
        /// <remarks>
        ///     Dictionary keys and values are treated as System.String
        /// </remarks>
        /// <param name="object">The objecto to be converted</param>
        /// <param name="prefix">An optinal string to add to dictionary keys.</param>
        /// <returns>
        ///     <see cref="Dictionary{TKey, TValue}"/>
        /// </returns>
        Dictionary<string, string> Convert(object @object, string prefix = "");
    }
}
