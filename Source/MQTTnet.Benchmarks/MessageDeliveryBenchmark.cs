// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class MessageDeliveryBenchmark : BaseBenchmark
    {
        List<MqttApplicationMessage> _topicPublishMessages;

        [Params(1, 5)]
        public int NumTopicsPerPublisher;

        [Params(1000, 10000)]
        public int NumPublishers;

        [Params(10)]
        public int NumSubscribers;

        [Params(5, 10, 20, 50)]
        public int NumSubscribedTopicsPerSubscriber;

        object _lockMsgCount;
        int _messagesReceivedCount;
        int _messagesExpectedCount;
        CancellationTokenSource _cancellationTokenSource;

        MqttServer _mqttServer;
        List<IMqttClient> _mqttSubscriberClients;
        Dictionary<string, IMqttClient> _mqttPublisherClientsByPublisherName;

        Dictionary<string, List<string>> _topicsByPublisher;
        Dictionary<string, string> _publisherByTopic;
        List<string> _allSubscribedTopics; // Keep track of the subset of topics that are subscribed


        [GlobalSetup]
        public void Setup()
        {
            _lockMsgCount = new object();

            Dictionary<string, List<string>> singleWildcardTopicsByPublisher;
            Dictionary<string, List<string>> multiWildcardTopicsByPublisher;

            TopicGenerator.Generate(NumPublishers, NumTopicsPerPublisher, out _topicsByPublisher, out singleWildcardTopicsByPublisher, out multiWildcardTopicsByPublisher);

            var topics = _topicsByPublisher.First().Value;
            _topicPublishMessages = new List<MqttApplicationMessage>();
            // Prepare messages, same for each publisher
            foreach (var topic in topics)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                    .Build();
                _topicPublishMessages.Add(message);
            }

            // Create server
            var serverFactory = new MqttServerFactory();
            var clientFactory = new MqttClientFactory();
            var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
            _mqttServer = serverFactory.CreateMqttServer(serverOptions);
            _mqttServer.StartAsync().GetAwaiter().GetResult();

            // Create publisher clients
            _mqttPublisherClientsByPublisherName = new Dictionary<string, IMqttClient>();
            foreach (var pt in _topicsByPublisher)
            {
                var publisherName = pt.Key;
                var mqttClient = clientFactory.CreateMqttClient();
                var publisherOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .WithClientId(publisherName)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                    .Build();
                mqttClient.ConnectAsync(publisherOptions).GetAwaiter().GetResult();
                _mqttPublisherClientsByPublisherName.Add(publisherName, mqttClient);
            }

            // Create subscriber clients
            _mqttSubscriberClients = new List<IMqttClient>();
            for (var i = 0; i < NumSubscribers; i++)
            {
                var mqttSubscriberClient = clientFactory.CreateMqttClient();
                _mqttSubscriberClients.Add(mqttSubscriberClient);

                var subscriberOptions = new MqttClientOptionsBuilder()
                       .WithTcpServer("localhost")
                       .WithClientId("subscriber" + i)
                       .Build();
                mqttSubscriberClient.ApplicationMessageReceivedAsync += r =>
                {
                    // count messages and signal cancellation when expected message count is reached
                    lock (_lockMsgCount)
                    {
                        ++_messagesReceivedCount;
                        if (_messagesReceivedCount == _messagesExpectedCount)
                        {
                            _cancellationTokenSource.Cancel();
                        }
                    }
                    return Task.CompletedTask;
                };
                mqttSubscriberClient.ConnectAsync(subscriberOptions).GetAwaiter().GetResult();
            }


            List<string> allTopics = new List<string>();
            _publisherByTopic = new Dictionary<string, string>();
            foreach (var t in _topicsByPublisher)
            {
                foreach (var topic in t.Value)
                {
                    _publisherByTopic.Add(topic, t.Key);
                    allTopics.Add(topic);
                }
            }

            // Subscribe to NumSubscribedTopics topics spread across all topics
            _allSubscribedTopics = new List<string>();

            var totalNumTopics = NumPublishers * NumTopicsPerPublisher;
            int topicIndexStep = totalNumTopics / (NumSubscribedTopicsPerSubscriber * NumSubscribers);
            if (topicIndexStep * NumSubscribedTopicsPerSubscriber * NumSubscribers != totalNumTopics)
            {
                throw new System.Exception(
                    String.Format("The total number of topics must be divisible by the number of subscribed topics across all subscribers. Total number of topics: {0}, topic step: {1}",
                    totalNumTopics, topicIndexStep
                    ));
            }

            var topicIndex = 0;
            foreach (var mqttSubscriber in _mqttSubscriberClients)
            {
                for (var i = 0; i < NumSubscribedTopicsPerSubscriber; ++i, topicIndex += topicIndexStep)
                {
                    var topic = allTopics[topicIndex];
                    _allSubscribedTopics.Add(topic);
                    var subOptions = new Client.MqttClientSubscribeOptionsBuilder().WithTopicFilter(
                        new Packets.MqttTopicFilter() { Topic = topic })
                        .Build();
                    mqttSubscriber.SubscribeAsync(subOptions).GetAwaiter().GetResult();
                }
            }

            Task.Delay(1000).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Publish messages and wait for messages sent to subscribers
        /// </summary>
        [Benchmark]
        public void DeliverMessages()
        {
            // There should be one message received per publish for each subscribed topic
            _messagesExpectedCount = NumSubscribedTopicsPerSubscriber * NumSubscribers;

            // Loop for a while and exchange messages

            _messagesReceivedCount = 0;

            _cancellationTokenSource = new CancellationTokenSource();

            // same payload for all messages
            var payload = new byte[] { 1, 2, 3, 4 };

            var tasks = new List<Task>();

            // publish a message for each subscribed topic
            foreach (var topic in _allSubscribedTopics)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .Build();
                // pick the correct publisher
                var publisherName = _publisherByTopic[topic];
                var publisherClient = _mqttPublisherClientsByPublisherName[publisherName];
                _ = publisherClient.PublishAsync(message);
            }

            // Wait one message per publish to be received by subscriber (in the subscriber's application message handler)
            try
            {
                Task.Delay(30000, _cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            catch
            {

            }

            _cancellationTokenSource.Dispose();

            if (_messagesReceivedCount < _messagesExpectedCount)
            {
                throw new Exception(string.Format("Messages Received Count mismatch, expected {0}, received {1}", _messagesExpectedCount, _messagesReceivedCount));
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var mp in _mqttPublisherClientsByPublisherName)
            {
                var mqttPublisherClient = mp.Value;
                mqttPublisherClient.DisconnectAsync().GetAwaiter().GetResult();
                mqttPublisherClient.Dispose();
            }
            _mqttPublisherClientsByPublisherName.Clear();

            foreach (var mqttSubscriber in _mqttSubscriberClients)
            {
                mqttSubscriber.DisconnectAsync().GetAwaiter().GetResult();
                mqttSubscriber.Dispose();
            }
            _mqttSubscriberClients.Clear();

            _mqttServer.StopAsync().GetAwaiter().GetResult();
            _mqttServer.Dispose();
            _mqttServer = null;
        }
    }
}
