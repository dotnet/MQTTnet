using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        /// <summary>
        /// Gets the internally used MQTT client.
        /// This property should be used with caution because manipulating the internal client might break the managed client.
        /// </summary>
        IMqttClient InternalClient { get; }

        /// <summary>
        /// Gets a value indicating whether the client is started or not.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Gets a value indicating whether the client is connected or not.
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Gets a pending application message count.
        /// </summary>
        int PendingApplicationMessagesCount { get; }
        
        /// <summary>
        /// Gets the MQTT client options.
        /// </summary>
        IManagedMqttClientOptions Options { get; }
        
        /// <summary>
        /// Gets or sets the connected handler.
        /// </summary>
        IMqttClientConnectedHandler ConnectedHandler { get; set; }

        /// <summary>
        /// Gets or sets the disconnected handler.
        /// </summary>
        IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        /// <summary>
        /// Gets or sets the application message process handler.
        /// </summary>
        IApplicationMessageProcessedHandler ApplicationMessageProcessedHandler { get; set; }

        /// <summary>
        /// Gets or sets the application message skipped handler.
        /// </summary>
        IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }

        /// <summary>
        /// Gets or sets the connecting failed handler.
        /// </summary>
        IConnectingFailedHandler ConnectingFailedHandler { get; set; }
        
        /// <summary>
        /// Gets or sets the synchronizing subscriptions failed handler.
        /// </summary>
        ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }

        /// <summary>
        /// Starts the client.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task StartAsync(IManagedMqttClientOptions options);

        /// <summary>
        /// Stops the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task StopAsync();

        /// <summary>
        /// Pings the server.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task PingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Subscribes the client.
        /// </summary>
        /// <param name="topicFilters">The topic filters.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters);

        /// <summary>
        /// Unsubscribes the client.
        /// </summary>
        /// <param name="topics">The topics.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task UnsubscribeAsync(IEnumerable<string> topics);

        /// <summary>
        /// Publishes a message from the client.
        /// </summary>
        /// <param name="applicationMessages">The application messages.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task PublishAsync(ManagedMqttApplicationMessage applicationMessages);
    }
}