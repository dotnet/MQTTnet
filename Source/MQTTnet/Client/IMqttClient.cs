using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.Client
{
    public interface IMqttClient : IDisposable
    {
        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;

        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;

        event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync;

        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;

        event Func<InspectMqttPacketEventArgs, Task> InspectPackageAsync;

        bool IsConnected { get; }

        MqttClientOptions Options { get; }

        Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default);

        Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default);

        Task PingAsync(CancellationToken cancellationToken = default);

        Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default);

        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default);

        Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default);

        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default);
    }
}