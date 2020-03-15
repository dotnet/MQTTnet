using BenchmarkDotNet.Attributes;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using MqttTopicFilterComparer = MQTTnet.Server.MqttTopicFilterComparer;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class TopicFilterComparerBenchmark
    {
        private List<TopicFilter> subscriptions = new List<TopicFilter>();
        private MqttSubscriptionIndex _subscriptionIndex = new MqttSubscriptionIndex();

        [GlobalSetup]
        public void Setup()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                var c = Convert.ToChar(i);
                if (char.IsLetter(c) && !char.IsUpper(c))
                {
                    subscriptions.Add(new TopicFilter() { Topic = new string(c, 1) });
                }
            }


            subscriptions.RemoveRange(1000, subscriptions.Count - 1000);

            foreach (var subscription in subscriptions)
            {
                MqttSubscriptionIndex.Subscribe(subscription, _subscriptionIndex);
            }

            Console.WriteLine($"{subscriptions.Count} subscriptions");
        }

        [Benchmark]
        public void LinearSearch()
        {
            foreach (var subscription in subscriptions)
            {
                MqttTopicFilterComparer.IsMatch("z/z", subscription.Topic);
            }
        }

        [Benchmark]
        public void IndexSearch()
        {
            _subscriptionIndex.GetQosLevels("z/z");
        }
    }
}
