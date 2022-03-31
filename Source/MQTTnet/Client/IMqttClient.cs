// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;

namespace MQTTnet.Client
{
    public interface IMqttClient
    {
        bool IsConnected { get; }
        MqttClientOptions Options { get; }

        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;
        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;
        event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync;
        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;
        event Func<InspectMqttPacketEventArgs, Task> InspectPackage;

        Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = null);
        Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = null);
        Task PingAsync(CancellationToken cancellationToken = null);
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = null);
        Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = null);
        Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = null);
        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = null);
    }
}