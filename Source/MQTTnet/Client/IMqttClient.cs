// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttClient : IDisposable
    {
        bool IsConnected { get; }
        MqttClientOptions Options { get; }

        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;
        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;
        event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync;
        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;
        event Func<InspectMqttPacketEventArgs, Task> InspectPackage;

        Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default);
        Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default);
        Task PingAsync(CancellationToken cancellationToken = default);
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default);
        Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default);
        Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default);
        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default);
    }
}