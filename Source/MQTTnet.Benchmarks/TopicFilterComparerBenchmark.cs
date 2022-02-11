// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using MQTTnet.Server;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class TopicFilterComparerBenchmark
    {
        static readonly char[] TopicLevelSeparator = { '/' };

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public void MqttTopicFilterComparer_10000_StringSplitMethod()
        {
            for (var i = 0; i < 10000; i++)
            {
                LegacyMethodByStringSplit("sport/tennis/player1", "sport/#");
                LegacyMethodByStringSplit("sport/tennis/player1/ranking", "sport/#/ranking");
                LegacyMethodByStringSplit("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                LegacyMethodByStringSplit("sport/tennis/player1", "sport/tennis/+");
                LegacyMethodByStringSplit("/finance", "+/+");
                LegacyMethodByStringSplit("/finance", "/+");
                LegacyMethodByStringSplit("/finance", "+");
            }
        }

        [Benchmark]
        public void MqttTopicFilterComparer_10000_LoopMethod()
        {
            for (var i = 0; i < 10000; i++)
            {
                MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/#");
                MqttTopicFilterComparer.Compare("sport/tennis/player1/ranking", "sport/#/ranking");
                MqttTopicFilterComparer.Compare("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/tennis/+");
                MqttTopicFilterComparer.Compare("/finance", "+/+");
                MqttTopicFilterComparer.Compare("/finance", "/+");
                MqttTopicFilterComparer.Compare("/finance", "+");
            }
        }

        static bool LegacyMethodByStringSplit(string topic, string filter)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

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
    }
}
