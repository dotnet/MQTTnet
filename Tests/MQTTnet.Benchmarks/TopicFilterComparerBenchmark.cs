using BenchmarkDotNet.Attributes;
using MQTTnet.Server;
using System;

namespace MQTTnet.Benchmarks
{
    [ClrJob]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class TopicFilterComparerBenchmark
    {
        private static readonly char[] TopicLevelSeparator = { '/' };

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
                MqttTopicFilterComparer.IsMatch("sport/tennis/player1", "sport/#");
                MqttTopicFilterComparer.IsMatch("sport/tennis/player1/ranking", "sport/#/ranking");
                MqttTopicFilterComparer.IsMatch("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
                MqttTopicFilterComparer.IsMatch("sport/tennis/player1", "sport/tennis/+");
                MqttTopicFilterComparer.IsMatch("/finance", "+/+");
                MqttTopicFilterComparer.IsMatch("/finance", "/+");
                MqttTopicFilterComparer.IsMatch("/finance", "+");
            }
        }

        private static bool LegacyMethodByStringSplit(string topic, string filter)
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
