using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class SubscribeBenchmark
    {
        MqttServer _mqttServer;
        MQTTnet.Client.MqttClient _mqttClient;

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
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
            _mqttServer.StopAsync().GetAwaiter().GetResult();
            _mqttServer.Dispose();
        }

        [Benchmark]
        public void Subscribe_10000_Topics()
        {
            foreach (var topic in _topics)
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                       .WithTopicFilter(topic, Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                       .Build();
                _mqttClient.SubscribeAsync(subscribeOptions).GetAwaiter().GetResult();
            }
        }
    }
}
