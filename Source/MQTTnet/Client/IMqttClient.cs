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
    public interface IMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the client is connected or not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the options.
        /// This contains all the set options for the client.
        /// </summary>
        IMqttClientOptions Options { get; }

        /// <summary>
        /// Gets or sets the connected handler that is fired after the client has connected to the server successfully.
        /// Hint: Initialize handlers before you connect the client to avoid issues.
        /// </summary>
        IMqttClientConnectedHandler ConnectedHandler { get; set; }

        /// <summary>
        /// Gets or sets the disconnected handler that is fired after the client has disconnected from the server.
        /// Hint: Initialize handlers before you connect the client to avoid issues.
        /// </summary>
        IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        /// <summary>
        /// Connects the client to the server using the specified options.
        /// </summary>
        /// <param name="options">The options that can be used on connect.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A result containing the authentication result information.</returns>
        Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        /// <param name="options">The options containing a disconnect reason.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Pings the server.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task PingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Sends extended authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="data">The extended data.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken);

        /// <summary>
        /// Subscribes the client to topics.
        /// </summary>
        /// <param name="options">The subscription options.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Unsubscribes the client from topics.
        /// </summary>
        /// <param name="options">The unsubscription options.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken);
    }
}