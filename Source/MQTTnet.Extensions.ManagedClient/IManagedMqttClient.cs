// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IDisposable
    {
        IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }
        IMqttClient InternalClient { get; }
        bool IsConnected { get; }
        bool IsStarted { get; }
        ManagedMqttClientOptions Options { get; }
        int PendingApplicationMessagesCount { get; }
        ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }

        event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync;
        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;
        event Func<EventArgs, Task> ConnectedAsync;
        event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync;
        event Func<EventArgs, Task> ConnectionStateChangedAsync;
        event Func<EventArgs, Task> DisconnectedAsync;

        Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage);
        Task EnqueueAsync(MqttApplicationMessage applicationMessage);
        Task PingAsync(CancellationToken cancellationToken);
        Task StartAsync(ManagedMqttClientOptions options);
        Task StopAsync();
        Task SubscribeAsync(ICollection<MqttTopicFilter> topicFilters);
        Task UnsubscribeAsync(ICollection<string> topics);
    }
}