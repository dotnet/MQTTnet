// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Benchmarks;

[MemoryDiagnoser]
public class MessageDeliveryBenchmark : BaseBenchmark
{
    List<string> _allSubscribedTopics; // Keep track of the subset of topics that are subscribed
    CancellationTokenSource _cancellationTokenSource;

    object _lockMsgCount;
    int _messagesExpectedCount;
    int _messagesReceivedCount;
    Dictionary<string, IMqttClient> _mqttPublisherClientsByPublisherName;
    MqttServer _mqttServer;
    List<IMqttClient> _mqttSubscriberClients;

    [Params(1000, 10000)] public int _numPublishers;

    [Params(5, 10, 20, 50)] public int _numSubscribedTopicsPerSubscriber;

    [Params(10)] public int _numSubscribers;

    [Params(1, 5)] public int _numTopicsPerPublisher;
    Dictionary<string, string> _publisherByTopic;
    Dictionary<string, List<string>> _topicsByPublisher;

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

    [Benchmark]
    public void DeliverMessages()
    {
        // There should be one message received per publish for each subscribed topic
        _messagesExpectedCount = _numSubscribedTopicsPerSubscriber * _numSubscribers;

        // Loop for a while and exchange messages

        _messagesReceivedCount = 0;

        _cancellationTokenSource = new CancellationTokenSource();

        // same payload for all messages
        var payload = new byte[] { 1, 2, 3, 4 };

        // publish a message for each subscribed topic
        foreach (var topic in _allSubscribedTopics)
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build();
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
            // Ignore all errors.
        }

        _cancellationTokenSource.Dispose();

        if (_messagesReceivedCount < _messagesExpectedCount)
        {
            throw new Exception($"Messages Received Count mismatch, expected {_messagesExpectedCount}, received {_messagesReceivedCount}");
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _lockMsgCount = new object();

        TopicGenerator.Generate(_numPublishers, _numTopicsPerPublisher, out _topicsByPublisher, out _, out _);

        // Create server
        var serverFactory = new MqttServerFactory();
        var clientFactory = new MqttClientFactory();
        var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
        _mqttServer = serverFactory.CreateMqttServer(serverOptions);
        _mqttServer.StartAsync().GetAwaiter().GetResult();

        // Create publisher clients
        _mqttPublisherClientsByPublisherName = [];
        foreach (var pt in _topicsByPublisher)
        {
            var publisherName = pt.Key;
            var mqttClient = clientFactory.CreateMqttClient();
            var publisherOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").WithClientId(publisherName).WithKeepAlivePeriod(TimeSpan.FromSeconds(30)).Build();
            mqttClient.ConnectAsync(publisherOptions).GetAwaiter().GetResult();
            _mqttPublisherClientsByPublisherName.Add(publisherName, mqttClient);
        }

        // Create subscriber clients
        _mqttSubscriberClients = new List<IMqttClient>();
        for (var i = 0; i < _numSubscribers; i++)
        {
            var mqttSubscriberClient = clientFactory.CreateMqttClient();
            _mqttSubscriberClients.Add(mqttSubscriberClient);

            var subscriberOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").WithClientId("subscriber" + i).Build();
            mqttSubscriberClient.ApplicationMessageReceivedAsync += _ =>
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


        var allTopics = new List<string>();
        _publisherByTopic = [];
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

        var totalNumTopics = _numPublishers * _numTopicsPerPublisher;
        var topicIndexStep = totalNumTopics / (_numSubscribedTopicsPerSubscriber * _numSubscribers);
        if (topicIndexStep * _numSubscribedTopicsPerSubscriber * _numSubscribers != totalNumTopics)
        {
            throw new Exception(
                $"The total number of topics must be divisible by the number of subscribed topics across all subscribers. Total number of topics: {totalNumTopics}, topic step: {topicIndexStep}");
        }

        var topicIndex = 0;
        foreach (var mqttSubscriber in _mqttSubscriberClients)
        {
            for (var i = 0; i < _numSubscribedTopicsPerSubscriber; ++i, topicIndex += topicIndexStep)
            {
                var topic = allTopics[topicIndex];
                _allSubscribedTopics.Add(topic);
                var subOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(new MqttTopicFilter { Topic = topic }).Build();
                mqttSubscriber.SubscribeAsync(subOptions).GetAwaiter().GetResult();
            }
        }

        Task.Delay(1000).GetAwaiter().GetResult();
    }
}