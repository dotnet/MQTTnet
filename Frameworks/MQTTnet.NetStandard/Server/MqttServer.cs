﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public class MqttServer : IMqttServer
    {
        private readonly ICollection<IMqttServerAdapter> _adapters;
        private readonly IMqttNetLogger _logger;

        private MqttClientSessionsManager _clientSessionsManager;
        private MqttRetainedMessagesManager _retainedMessagesManager;
        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _adapters = adapters.ToList();
        }

        public event EventHandler<MqttServerStartedEventArgs> Started;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;
        public event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public IMqttServerOptions Options { get; private set; }

        public Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            return _clientSessionsManager.GetConnectedClientsAsync();
        }

        public Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
        }

        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The server is not started.");

            foreach (var applicationMessage in applicationMessages)
            {
                await _clientSessionsManager.DispatchApplicationMessageAsync(null, applicationMessage);
            }
        }

        public async Task StartAsync(IMqttServerOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (_cancellationTokenSource != null) throw new InvalidOperationException("The server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _retainedMessagesManager = new MqttRetainedMessagesManager(Options, _logger);
            await _retainedMessagesManager.LoadMessagesAsync();

            _clientSessionsManager = new MqttClientSessionsManager(Options, _retainedMessagesManager, _logger)
            {
                ClientConnectedCallback = OnClientConnected,
                ClientDisconnectedCallback = OnClientDisconnected,
                ClientSubscribedTopicCallback = OnClientSubscribedTopic,
                ClientUnsubscribedTopicCallback = OnClientUnsubscribedTopic,
                ApplicationMessageReceivedCallback = OnApplicationMessageReceived
            };

            foreach (var adapter in _adapters)
            {
                adapter.ClientAccepted += OnClientAccepted;
                await adapter.StartAsync(Options);
            }

            _logger.Info<MqttServer>("Started.");
            Started?.Invoke(this, new MqttServerStartedEventArgs());
        }

        public async Task StopAsync()
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                _cancellationTokenSource.Cancel(false);
                _cancellationTokenSource.Dispose();

                foreach (var adapter in _adapters)
                {
                    adapter.ClientAccepted -= OnClientAccepted;
                    await adapter.StopAsync();
                }

                await _clientSessionsManager.StopAsync();

                _logger.Info<MqttServer>("Stopped.");
            }
            finally
            {
                _clientSessionsManager?.Dispose();
                _retainedMessagesManager?.Dispose();

                _cancellationTokenSource = null;
                _retainedMessagesManager = null;
                _clientSessionsManager = null;
            }
        }

        private void OnClientConnected(ConnectedMqttClient client)
        {
            _logger.Info<MqttServer>("Client '{0}': Connected.", client.ClientId);
            ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(client));
        }

        private void OnClientDisconnected(ConnectedMqttClient client)
        {
            _logger.Info<MqttServer>("Client '{0}': Disconnected.", client.ClientId);
            ClientDisconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(client));
        }

        private void OnClientSubscribedTopic(string clientId, TopicFilter topicFilter)
        {
            ClientSubscribedTopic?.Invoke(this, new MqttClientSubscribedTopicEventArgs(clientId, topicFilter));
        }

        private void OnClientUnsubscribedTopic(string clientId, string topicFilter)
        {
            ClientUnsubscribedTopic?.Invoke(this, new MqttClientUnsubscribedTopicEventArgs(clientId, topicFilter));
        }

        private void OnApplicationMessageReceived(string clientId, MqttApplicationMessage applicationMessage)
        {
            ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(clientId, applicationMessage));
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs eventArgs)
        {
            eventArgs.SessionTask = Task.Run(
                async () => await _clientSessionsManager.RunSessionAsync(eventArgs.Client, _cancellationTokenSource.Token).ConfigureAwait(false),
                _cancellationTokenSource.Token);
        }
    }
}
