// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class MqttConnectionManager
    {
        readonly MqttNetSourceLogger _logger;
        readonly IMqttClient _mqttClient;

        bool _wasConnected;

        public MqttConnectionManager(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource<MqttConnectionManager>();

            _mqttClient.DisconnectedAsync += OnClientDisconnected;
        }

        public AsyncEvent<MqttClientConnectedEventArgs> ConnectedEvent { get; } = new AsyncEvent<MqttClientConnectedEventArgs>();

        public AsyncEvent<ConnectingFailedEventArgs> ConnectingFailedEvent { get; } = new AsyncEvent<ConnectingFailedEventArgs>();

        public AsyncEvent<MqttClientDisconnectedEventArgs> DisconnectedEvent { get; } = new AsyncEvent<MqttClientDisconnectedEventArgs>();

        public async Task<MaintainConnectionResult> MaintainConnection(ManagedMqttClientOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (_wasConnected)
            {
                try
                {
                    // The only valid way for testing the connection is to send a PING. So we start with this
                    // in order to check if reconnecting etc. is require or not.
                    await _mqttClient.PingAsync(cancellationToken).ConfigureAwait(false);

                    // The client is properly connection. No further action required.
                    return MaintainConnectionResult.NoChange;
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while checking connection.");
                }
            }

            MqttClientConnectResult connectResult = null;
            try
            {
                // The PING was not successful so that there is no working connection. So we connect.
                connectResult = await _mqttClient.ConnectAsync(options.ClientOptions, cancellationToken).ConfigureAwait(false);

                _wasConnected = true;
                _logger.Info("Connected.");

                if (ConnectedEvent.HasHandlers)
                {
                    var eventArgs = new MqttClientConnectedEventArgs(connectResult);
                    await ConnectedEvent.TryInvokeAsync(eventArgs, _logger).ConfigureAwait(false);
                }

                return MaintainConnectionResult.ConnectedEstablished;
            }
            catch (Exception exception)
            {
                if (!(exception is OperationCanceledException))
                {
                    _logger.Error(exception, "Error while connecting.");
                }

                if (ConnectingFailedEvent.HasHandlers)
                {
                    var eventArgs = new ConnectingFailedEventArgs(connectResult, exception);
                    await ConnectingFailedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }
            }

            _wasConnected = false;
            return MaintainConnectionResult.NotConnected;
        }

        Task OnClientDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            _wasConnected = false;
            return DisconnectedEvent?.InvokeAsync(arg);
        }
    }
}