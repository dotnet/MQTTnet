// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IDisposable
    {
        event Func<ApplicationMessageDroppedEventArgs, Task> ApplicationMessageDroppedAsync;

        event Func<EventArgs, Task> ApplicationMessageEnqueueingAsync;

        event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync;

        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;

        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;

        event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync;

        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;

        event Func<SubscribeProcessedEventArgs, Task> SubscribeProcessedAsync;

        event Func<UnsubscribeProcessedEventArgs, Task> UnsubscribeProcessedAsync;

        int EnqueuedApplicationMessagesCount { get; }

        IMqttClient InternalClient { get; }

        bool IsConnected { get; }

        bool IsStarted { get; }

        ManagedMqttClientOptions Options { get; }

        Task EnqueueAsync(MqttApplicationMessage applicationMessage);

        Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage);

        Task PingAsync(CancellationToken cancellationToken = default);

        Task StartAsync(ManagedMqttClientOptions options);

        Task StopAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken);

        Task SubscribeAsync(MqttClientSubscribeOptions options);

        Task UnsubscribeAsync(MqttClientUnsubscribeOptions options);
    }
}