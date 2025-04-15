// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet
{
    public static class MqttTopicFilterComparer
    {
        public const char LevelSeparator = '/';
        public const char MultiLevelWildcard = '#';
        public const char SingleLevelWildcard = '+';
        public const char ReservedTopicPrefix = '$';

        public static unsafe MqttTopicFilterCompareResult Compare(string topic, string filter)
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
           
            fixed (char* topicPointer = topic)
            fixed (char* filterPointer = filter)
            {
                if (filterLength > topicLength)
                {
                    // It is impossible to create a filter which is longer than the actual topic.
                    // The only way this can happen is when the last char is a wildcard char.
                    // sensor/7/temperature >> sensor/7/temperature = Equal
                    // sensor/+/temperature >> sensor/7/temperature = Equal
                    // sensor/7/+           >> sensor/7/temperature = Shorter
                    // sensor/#             >> sensor/7/temperature = Shorter
                    var lastFilterChar = filterPointer[filterLength - 1];
                    if (lastFilterChar != MultiLevelWildcard && lastFilterChar != SingleLevelWildcard)
                    {
                        return MqttTopicFilterCompareResult.NoMatch;    
                    }
                }
                
                var isMultiLevelFilter = filterPointer[filterLength - 1] == MultiLevelWildcard;
                var isReservedTopic = topicPointer[0] == ReservedTopicPrefix;

                if (isReservedTopic && filterLength == 1 && isMultiLevelFilter)
                {
                    // It is not allowed to receive i.e. '$foo/bar' with filter '#'.
                    return MqttTopicFilterCompareResult.NoMatch;
                }

                if (isReservedTopic && filterPointer[0] == SingleLevelWildcard)
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
                    if (filterPointer[filterOffset] == MultiLevelWildcard && filterOffset != filterLength - 1)
                    {
                        return MqttTopicFilterCompareResult.FilterInvalid;
                    }

                    if (filterPointer[filterOffset] == topicPointer[topicOffset])
                    {
                        if (topicOffset == topicLength - 1)
                        {
                            // Check for e.g. "foo" matching "foo/#"
                            if (filterOffset == filterLength - 3 && filterPointer[filterOffset + 1] == LevelSeparator && isMultiLevelFilter)
                            {
                                return MqttTopicFilterCompareResult.IsMatch;
                            }

                            // Check for e.g. "foo/" matching "foo/#"
                            if (filterOffset == filterLength - 2 && filterPointer[filterOffset] == LevelSeparator && isMultiLevelFilter)
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

                        if (endOfTopic && filterOffset == filterLength - 1 && filterPointer[filterOffset] == SingleLevelWildcard)
                        {
                            if (filterOffset > 0 && filterPointer[filterOffset - 1] != LevelSeparator)
                            {
                                return MqttTopicFilterCompareResult.FilterInvalid;
                            }

                            return MqttTopicFilterCompareResult.IsMatch;
                        }
                    }
                    else
                    {
                        if (filterPointer[filterOffset] == SingleLevelWildcard)
                        {
                            // Check for invalid "+foo" or "a/+foo" subscription
                            if (filterOffset > 0 && filterPointer[filterOffset - 1] != LevelSeparator)
                            {
                                return MqttTopicFilterCompareResult.FilterInvalid;
                            }

                            // Check for bad "foo+" or "foo+/a" subscription
                            if (filterOffset < filterLength - 1 && filterPointer[filterOffset + 1] != LevelSeparator)
                            {
                                return MqttTopicFilterCompareResult.FilterInvalid;
                            }

                            filterOffset++;
                            while (topicOffset < topicLength && topicPointer[topicOffset] != LevelSeparator)
                            {
                                topicOffset++;
                            }

                            if (topicOffset == topicLength && filterOffset == filterLength)
                            {
                                return MqttTopicFilterCompareResult.IsMatch;
                            }
                        }
                        else if (filterPointer[filterOffset] == MultiLevelWildcard)
                        {
                            if (filterOffset > 0 && filterPointer[filterOffset - 1] != LevelSeparator)
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
                            if (filterOffset > 0 && filterOffset + 2 == filterLength && topicOffset == topicLength && filterPointer[filterOffset - 1] == SingleLevelWildcard &&
                                filterPointer[filterOffset] == LevelSeparator && isMultiLevelFilter)
                            {
                                return MqttTopicFilterCompareResult.IsMatch;
                            }

                            return MqttTopicFilterCompareResult.NoMatch;
                        }
                    }
                }
            }

            return MqttTopicFilterCompareResult.NoMatch;
        }

        public static bool ContainsWildcards(string topicFilter)
        {
            if (topicFilter[topicFilter.Length - 1] == MultiLevelWildcard)
            {
                return true;
            }

            return topicFilter.Contains(SingleLevelWildcard);
        }
    }
}