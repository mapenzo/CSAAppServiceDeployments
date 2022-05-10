using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCost_to_LogAnalytics
{
    internal class Constants
    {
        internal static class Json
        {
            public const string Default = @"{
                        'dataset': {
                            'aggregation': {
                            'totalCost': {
                                'function': 'Sum',
                                'name': 'PreTaxCost'
                            }
                        },
                        'granularity': 'Daily',
                        'grouping': [
                            {
                                'name': 'ResourceId',
                                'type': 'Dimension'
                            },
                            {
                                'name': 'ResourceType',
                                'type': 'dimension'
                            },
                            {
                                'name': 'SubscriptionName',
                                'type': 'dimension'
                            },
                            {
                                'name': 'ResourceGroup',
                                'type': 'dimension'
                            }
                        ]
                    },
                    'timePeriod': {
                        'from': '{start}@',
                        'to': '{end}@'
                    },
                    'timeframe': 'Custom',
                    'type': 'Usage'
                }";
        }
    }
}
