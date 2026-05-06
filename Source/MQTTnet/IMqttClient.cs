using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet;

/// <summary>
/// Represents an MQTT client.
/// <para>
/// Instances of <see cref="IMqttClient" /> are safe for concurrent use: <see cref="PublishAsync" />,
/// <see cref="SubscribeAsync" />, <see cref="UnsubscribeAsync" /> and <see cref="PingAsync" /> may be
/// invoked from multiple threads at the same time. Outgoing packets are serialized internally so
/// that they reach the network in well-formed order, and incoming acknowledgements are routed back
/// to the corresponding caller via the packet identifier.
/// </para>
/// <para>
/// <see cref="ConnectAsync" /> and <see cref="DisconnectAsync" /> must not be called concurrently
/// with each other or with publish/subscribe operations. Coordinate connection lifecycle changes on
/// a single thread.
/// </para>
/// </summary>
public interface IMqttClient : IDisposable
{
    event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;

    event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;

    event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync;

    event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;

    event Func<InspectMqttPacketEventArgs, Task> InspectPacketAsync;

    bool IsConnected { get; }

    MqttClientOptions Options { get; }

    Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default);

    Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default);

    Task PingAsync(CancellationToken cancellationToken = default);

    Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default);

    Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default);

    Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default);

    Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default);
}