using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using MQTTnet.Server.Status;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttHostedServer : IMqttServer, IHostedService
    {
        readonly MqttServer _mqttServer;
        readonly IMqttServerOptions _options;

        public MqttHostedServer(IMqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _mqttServer = new MqttServer(adapters, logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = _mqttServer.StartAsync(_options);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _mqttServer.StopAsync();
        }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => _mqttServer.ApplicationMessageReceivedHandler;
            set => _mqttServer.ApplicationMessageReceivedHandler = value;
        }

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => _mqttServer.ApplicationMessageReceivedAsync += value;
            remove => _mqttServer.ApplicationMessageReceivedAsync -= value;
        }
        
        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            return _mqttServer.PublishAsync(applicationMessage, cancellationToken);
        }

        public void Dispose()
        {
            _mqttServer.Dispose();
        }

        public bool IsStarted => _mqttServer.IsStarted;

        public IMqttServerStartedHandler StartedHandler
        {
            get => _mqttServer.StartedHandler;
            set => _mqttServer.StartedHandler = value;
        }

        public IMqttServerStoppedHandler StoppedHandler
        {
            get => _mqttServer.StoppedHandler;
            set => _mqttServer.StoppedHandler = value;
        }

        public IMqttServerClientConnectedHandler ClientConnectedHandler
        {
            get => _mqttServer.ClientConnectedHandler;
            set => _mqttServer.ClientConnectedHandler = value;
        }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler
        {
            get => _mqttServer.ClientDisconnectedHandler;
            set => _mqttServer.ClientDisconnectedHandler = value;
        }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler
        {
            get => _mqttServer.ClientSubscribedTopicHandler;
            set => _mqttServer.ClientSubscribedTopicHandler = value;
        }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler
        {
            get => _mqttServer.ClientUnsubscribedTopicHandler;
            set => _mqttServer.ClientUnsubscribedTopicHandler = value;
        }

        public IMqttServerOptions Options => _mqttServer.Options;

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            return _mqttServer.GetClientStatusAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            return _mqttServer.GetSessionStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            return _mqttServer.GetRetainedApplicationMessagesAsync();
        }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            return _mqttServer.ClearRetainedApplicationMessagesAsync();
        }

        public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            return _mqttServer.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            return _mqttServer.UnsubscribeAsync(clientId, topicFilters);
        }

        public Task StartAsync(IMqttServerOptions options)
        {
            return _mqttServer.StartAsync(options);
        }

        public Task StopAsync()
        {
            return _mqttServer.StopAsync();
        }
    }
}