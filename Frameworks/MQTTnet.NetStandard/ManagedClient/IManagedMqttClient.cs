﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        bool IsConnected { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        event EventHandler<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed;

        Task StartAsync(IManagedMqttClientOptions options);
        Task StopAsync();

        Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topics);
    }
}