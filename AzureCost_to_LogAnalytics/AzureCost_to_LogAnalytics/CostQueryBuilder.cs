namespace AzureCost_to_LogAnalytics
{
    internal class CostQueryBuilder
    {
        public static string Build(string from, string to)
        {
            return @"{
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
                        'from': '" + from + @"',
                        'to': '" + to + @"'
                    },
                    'timeframe': 'Custom',
                    'type': 'Usage'
                }";
        }
    }
}
