using AzureCost_to_LogAnalytics.Extensions.Extensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AzureCost_to_LogAnalytics.Services.Common
{
    public class DictionaryConverter : IDictionaryConverter
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, Func<object, object>>> CachedProperties;

        static DictionaryConverter()
            => CachedProperties = new ConcurrentDictionary<Type, Dictionary<PropertyInfo, Func<object, object>>>();

        public Dictionary<string, string> Convert(object @object, string prefix = "")
        {
            return ExecuteInternal(@object, prefix: prefix);
        }

        private static Dictionary<string, string> ExecuteInternal(
            object @object,
            Dictionary<string, string> dictionary = default,
            string prefix = "")
        {
            dictionary ??= new Dictionary<string, string>();
            var type = @object.GetType();
            var properties = GetProperties(type);

            foreach (var (property, getter) in properties)
            {
                var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";
                var value = getter(@object);

                if (value == null)
                {
                    dictionary.Add(key, null);
                    continue;
                }

                if (property.PropertyType.IsValueTypeOrString())
                {
                    dictionary.Add(key, value.ToStringValueType());
                }
                else
                {
                    if (value is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            var itemKey = $"{key}";
                            var itemType = item.GetType();
                            if (itemType.IsValueTypeOrString())
                            {
                                dictionary.Add(itemKey, item.ToStringValueType());
                            }
                            else
                            {
                                ExecuteInternal(item, dictionary, itemKey);
                            }
                        }
                    }
                    else
                    {
                        ExecuteInternal(value, dictionary, key);
                    }
                }
            }

            return dictionary;
        }

        private static Dictionary<PropertyInfo, Func<object, object>> GetProperties(Type type)
        {
            if (CachedProperties.TryGetValue(type, out var properties))
            {
                return properties;
            }

            CacheProperties(type);
            return CachedProperties[type];
        }

        private static void CacheProperties(Type type)
        {
            if (CachedProperties.ContainsKey(type))
            {
                return;
            }

            CachedProperties[type] = new Dictionary<PropertyInfo, Func<object, object>>();
            var properties = type.GetProperties().Where(x => x.CanRead);
            foreach (var propertyInfo in properties)
            {
                var getter = CompilePropertyGetter(propertyInfo);

                CachedProperties[type].Add(propertyInfo, getter);

                if (propertyInfo.PropertyType.IsIEnumerable())
                {
                    var types = propertyInfo.PropertyType.GetGenericArguments();
                    foreach (var genericType in types)
                    {
                        if (!genericType.IsValueTypeOrString())
                        {
                            CacheProperties(genericType);
                        }
                    }
                }
                else
                {
                    CacheProperties(propertyInfo.PropertyType);
                }
            }
        }

        // Inspired by Zanid Haytam
        // https://blog.zhaytam.com/2020/11/17/expression-trees-property-getter/
        private static Func<object, object> CompilePropertyGetter(PropertyInfo property)
        {
            var objectType = typeof(object);

            var objectParameter = Expression.Parameter(objectType);

            var castExpression = Expression.TypeAs(objectParameter, property.DeclaringType);

            var convertExpression = Expression.Convert(
                Expression.Property(castExpression, property),
                objectType);

            return Expression.Lambda<Func<object, object>>(
                convertExpression,
                objectParameter).Compile();
        }
    }
}
