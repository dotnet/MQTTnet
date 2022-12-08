// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class UnsubscribeBenchmark : BaseBenchmark 
    {
        MqttServer _mqttServer;
        IMqttClient _mqttClient;

        const int NumPublishers = 1;
        const int NumTopicsPerPublisher = 10000;

        List<string> _topics;

        [GlobalSetup]
        public void Setup()
        {
            TopicGenerator.Generate(NumPublishers, NumTopicsPerPublisher, out var topicsByPublisher, out var singleWildcardTopicsByPublisher, out var multiWildcardTopicsByPublisher);
            _topics = topicsByPublisher.Values.First();

            var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();

            var factory = new MqttFactory();
            _mqttServer = factory.CreateMqttServer(serverOptions);
            _mqttClient = factory.CreateMqttClient();

            _mqttServer.StartAsync().GetAwaiter().GetResult();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost").Build();

            _mqttClient.ConnectAsync(clientOptions).GetAwaiter().GetResult();

            foreach (var topic in _topics)
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                       .WithTopicFilter(topic)
                       .Build();
                _mqttClient.SubscribeAsync(subscribeOptions).GetAwaiter().GetResult();
            }
        }

        [Benchmark]
        public void Unsubscribe_10000_Topics()
        {
            foreach (var topic in _topics)
            {
                var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                       .WithTopicFilter(topic)
                       .Build();
                _mqttClient.UnsubscribeAsync(unsubscribeOptions).GetAwaiter().GetResult();
            }
        }
    }
}
