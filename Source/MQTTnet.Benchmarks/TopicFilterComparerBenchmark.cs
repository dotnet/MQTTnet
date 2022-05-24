// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class TopicFilterComparerBenchmark
    {
        static readonly char[] TopicLevelSeparator = { '/' };

        readonly string _longTopic =
            "AAAAAAAAAAAAAssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssshhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";

        [Benchmark]
        public void MqttTopicFilterComparer_10000_LoopMethod()
        {
            for (var i = 0; i < 100000; i++)
            {
                MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/#");
                MqttTopicFilterComparer.Compare("sport/tennis/player1/ranking", "sport/#/ranking");
                MqttTopicFilterComparer.Compare("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/tennis/+");
                MqttTopicFilterComparer.Compare("/finance", "+/+");
                MqttTopicFilterComparer.Compare("/finance", "/+");
                MqttTopicFilterComparer.Compare("/finance", "+");
                MqttTopicFilterComparer.Compare(_longTopic, _longTopic);
            }
        }

        [Benchmark]
        public void MqttTopicFilterComparer_10000_LoopMethod_Without_Pointer()
        {
            for (var i = 0; i < 100000; i++)
            {
                MqttTopicFilterComparerWithoutPointer.Compare("sport/tennis/player1", "sport/#");
                MqttTopicFilterComparerWithoutPointer.Compare("sport/tennis/player1/ranking", "sport/#/ranking");
                MqttTopicFilterComparerWithoutPointer.Compare("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                MqttTopicFilterComparerWithoutPointer.Compare("sport/tennis/player1", "sport/tennis/+");
                MqttTopicFilterComparerWithoutPointer.Compare("/finance", "+/+");
                MqttTopicFilterComparerWithoutPointer.Compare("/finance", "/+");
                MqttTopicFilterComparerWithoutPointer.Compare("/finance", "+");
                MqttTopicFilterComparerWithoutPointer.Compare(_longTopic, _longTopic);
            }
        }

        [Benchmark]
        public void MqttTopicFilterComparer_10000_StringSplitMethod()
        {
            for (var i = 0; i < 100000; i++)
            {
                LegacyMethodByStringSplit("sport/tennis/player1", "sport/#");
                LegacyMethodByStringSplit("sport/tennis/player1/ranking", "sport/#/ranking");
                LegacyMethodByStringSplit("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                LegacyMethodByStringSplit("sport/tennis/player1", "sport/tennis/+");
                LegacyMethodByStringSplit("/finance", "+/+");
                LegacyMethodByStringSplit("/finance", "/+");
                LegacyMethodByStringSplit("/finance", "+");
                MqttTopicFilterComparer.Compare(_longTopic, _longTopic);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        static bool LegacyMethodByStringSplit(string topic, string filter)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (string.Equals(topic, filter, StringComparison.Ordinal))
            {
                return true;
            }

            var fragmentsTopic = topic.Split(TopicLevelSeparator, StringSplitOptions.None);
            var fragmentsFilter = filter.Split(TopicLevelSeparator, StringSplitOptions.None);

            // # > In either case it MUST be the last character specified in the Topic Filter [MQTT-4.7.1-2].
            for (var i = 0; i < fragmentsFilter.Length; i++)
            {
                if (fragmentsFilter[i] == "+")
                {
                    continue;
                }

                if (fragmentsFilter[i] == "#")
                {
                    return true;
                }

                if (i >= fragmentsTopic.Length)
                {
                    return false;
                }

                if (!string.Equals(fragmentsFilter[i], fragmentsTopic[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return fragmentsTopic.Length == fragmentsFilter.Length;
        }

        public static class MqttTopicFilterComparerWithoutPointer
        {
            public const char LevelSeparator = '/';
            public const char MultiLevelWildcard = '#';
            public const char SingleLevelWildcard = '+';
            public const char ReservedTopicPrefix = '$';

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

                var topicPointer = topic;
                var filterPointer = filter;

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

                return MqttTopicFilterCompareResult.NoMatch;
            }
        }
    }
}