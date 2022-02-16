// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.LowLevelClient
{
    public sealed class LowLevelMqttClient : IDisposable
    {
        readonly IMqttClientAdapterFactory _clientAdapterFactory;
        readonly AsyncEvent<InspectMqttPacketEventArgs> _inspectPacketEvent = new AsyncEvent<InspectMqttPacketEventArgs>();
        readonly MqttNetSourceLogger _logger;

        readonly IMqttNetLogger _rootLogger;

        IMqttChannelAdapter _adapter;

        public LowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory, IMqttNetLogger logger)
        {
            _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));

            _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(LowLevelMqttClient));
        }

        public event Func<InspectMqttPacketEventArgs, Task> InspectPackage
        {
            add => _inspectPacketEvent.AddHandler(value);
            remove => _inspectPacketEvent.RemoveHandler(value);
        }

        public bool IsConnected => _adapter != null;

        public async Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (_adapter != null)
            {
                throw new InvalidOperationException("Low level MQTT client is already connected. Disconnect first before connecting again.");
            }

            var newAdapter = _clientAdapterFactory.CreateClientAdapter(options, new MqttPacketInspector(_inspectPacketEvent, _rootLogger), _rootLogger);

            try
            {
                _logger.Verbose("Trying to connect with server '{0}'.", options.ChannelOptions);
                await newAdapter.ConnectAsync(cancellationToken).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");
            }
            catch (Exception)
            {
                _adapter?.Dispose();
                throw;
            }

            _adapter = newAdapter;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await SafeDisconnect(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _adapter?.Dispose();
            _adapter = null;
        }

        public async Task<MqttPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            var adapter = _adapter;
            if (adapter == null)
            {
                throw new InvalidOperationException("Low level MQTT client is not connected.");
            }
            
            try
            {
                return await adapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await SafeDisconnect(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public async Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            if (packet is null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var adapter = _adapter;
            if (adapter == null)
            {
                throw new InvalidOperationException("Low level MQTT client is not connected.");
            }

            try
            {
                await adapter.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await SafeDisconnect(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        async Task SafeDisconnect(CancellationToken cancellationToken)
        {
            try
            {
                var adapter = _adapter;
                if (adapter == null)
                {
                    return;
                }
                
                await adapter.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting low level MQTT client.");
            }
            finally
            {
                Dispose();
            }
        }
    }
}