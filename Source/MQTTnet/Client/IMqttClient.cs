using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttClient : IMqttApplicationMessageReceiver, IMqttApplicationMessagePublisher, IDisposable
    {
        bool IsConnected { get; }

        IMqttClientOptions Options { get; }

        /// <summary>
        /// Gets or sets the connected handler that is fired after the client has connected to the server successfully.
        /// Hint: Initialize handlers before you connect the client to avoid issues.
        /// </summary>
        IMqttClientConnectedHandler ConnectedHandler { get; set; }

        /// <summary>
        /// Fired when the client is connected.
        /// </summary>
        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;
        
        /// <summary>
        /// Gets or sets the disconnected handler that is fired after the client has disconnected from the server.
        /// Hint: Initialize handlers before you connect the client to avoid issues.
        /// </summary>
        IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        /// <summary>
        /// Fired when the client is disconnected.
        /// </summary>
        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;
        
        Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken = default);

        Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default);

        Task PingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends extended authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="data">The extended data.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default);

        Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default);

        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default);
    }
}