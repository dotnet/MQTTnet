namespace MQTTnet.Server.Internal
{
    public static class MqttTopicFilterComparer
    {
        const char LevelSeparator = '/';
        const char MultiLevelWildcard = '#';
        const char SingleLevelWildcard = '+';
        const char ReservedTopicPrefix = '$';

        public static MqttTopicFilterCompareResult Compare(string topic, string filter)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return MqttTopicFilterCompareResult.TopicInvalid;
            }

            if (string.IsNullOrEmpty(filter))
            {
                return MqttTopicFilterCompareResult.FilterInvalid;
            }
            
            var filterOffset = 0;
            var filterLength = filter.Length;

            var topicOffset = 0;
            var topicLength = topic.Length;

            var isMultiLevelFilter = filter[filterLength - 1] == MultiLevelWildcard;
            var isReservedTopic = topic[0] == ReservedTopicPrefix;

            if (isReservedTopic && filterLength == 1 && isMultiLevelFilter)
            {
                // It is not allowed to receive i.e. '$foo/bar' with filter '#'.
                return MqttTopicFilterCompareResult.NoMatch;
            }
            
            if (isReservedTopic && filter[0] == SingleLevelWildcard)
            {
                // It is not allowed to receive i.e. '$SYS/monitor/Clients' with filter '+/monitor/Clients'.
                return MqttTopicFilterCompareResult.NoMatch;
            }

            if (filterLength == 1 && isMultiLevelFilter)
            {
                // Filter '#' matches basically everything.
                return MqttTopicFilterCompareResult.IsMatch;
            }
            
            // Go through the filter char by char.
            while (filterOffset < filterLength && topicOffset < topicLength)
            {
                // Check if the current char is a multi level wildcard. The char is only allowed
                // at the very las position.
                if (filter[filterOffset] == MultiLevelWildcard && filterOffset != filterLength - 1)
                {
                    return MqttTopicFilterCompareResult.FilterInvalid;
                }
                
                if (filter[filterOffset] == topic[topicOffset])
                {
                    if (topicOffset == topicLength - 1)
                    {
                        // Check for e.g. "foo" matching "foo/#"
                        if (filterOffset == filterLength - 3 &&
                            filter[filterOffset + 1] == LevelSeparator &&
                            isMultiLevelFilter)
                        {
                            return MqttTopicFilterCompareResult.IsMatch;
                        }

                        // Check for e.g. "foo/" matching "foo/#"
                        if (filterOffset == filterLength - 2 && 
                            filter[filterOffset] == LevelSeparator &&
                            isMultiLevelFilter)
                        {
                            return MqttTopicFilterCompareResult.IsMatch;
                        }
                    }

                    filterOffset++;
                    topicOffset++;

                    // Check if the end was reached and i.e. "foo/bar" matches "foo/bar"
                    if (filterOffset == filterLength && topicOffset == topicLength)
                    {
                        return MqttTopicFilterCompareResult.IsMatch;
                    }

                    var endOfTopic = topicOffset == topicLength;
                    
                    if (endOfTopic && filterOffset == filterLength - 1 && filter[filterOffset] == SingleLevelWildcard)
                    {
                        if (filterOffset > 0 && filter[filterOffset - 1] != LevelSeparator)
                        {
                            // Invalid filter.
                            return MqttTopicFilterCompareResult.FilterInvalid;
                        }

                        return MqttTopicFilterCompareResult.IsMatch;
                    }
                }
                else
                {
                    if (filter[filterOffset] == SingleLevelWildcard)
                    {
                        // Check for invalid "+foo" or "a/+foo" subscription
                        if (filterOffset > 0 && filter[filterOffset - 1] != LevelSeparator)
                        {
                            return MqttTopicFilterCompareResult.FilterInvalid;
                        }

                        // Check for bad "foo+" or "foo+/a" subscription
                        if (filterOffset < filterLength - 1 && filter[filterOffset + 1] != LevelSeparator)
                        {
                            return MqttTopicFilterCompareResult.FilterInvalid;
                        }

                        filterOffset++;
                        while (topicOffset < topicLength && topic[topicOffset] != LevelSeparator)
                        {
                            topicOffset++;
                        }

                        if (topicOffset == topicLength && filterOffset == filterLength)
                        {
                            return MqttTopicFilterCompareResult.IsMatch;
                        }
                    }
                    else if (filter[filterOffset] == MultiLevelWildcard)
                    {
                        if (filterOffset > 0 && filter[filterOffset - 1] != LevelSeparator)
                        {
                            return MqttTopicFilterCompareResult.FilterInvalid;
                        }

                        if (filterOffset + 1 != filterLength)
                        {
                            return MqttTopicFilterCompareResult.FilterInvalid;
                        }

                        return MqttTopicFilterCompareResult.IsMatch;
                    }
                    else
                    {
                        // Check for e.g. "foo/bar" matching "foo/+/#".
                        if (filterOffset > 0 &&
                            filterOffset + 2 == filterLength &&
                            topicOffset == topicLength &&
                            filter[filterOffset - 1] == SingleLevelWildcard &&
                            filter[filterOffset] == LevelSeparator &&
                            isMultiLevelFilter)
                        {
                            return MqttTopicFilterCompareResult.IsMatch;
                        }

                        return MqttTopicFilterCompareResult.NoMatch;
                    }
                }
            }

            return MqttTopicFilterCompareResult.NoMatch;
        }
    }
}