using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.LowLevelClient
{
    public sealed class LowLevelMqttClient : ILowLevelMqttClient
    {
        readonly IMqttNetScopedLogger _logger;
        readonly IMqttClientAdapterFactory _clientAdapterFactory;

        IMqttChannelAdapter _adapter;
        IMqttClientOptions _options;

        public LowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory, IMqttNetLogger logger)
        {
            _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));

            if (logger is null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(LowLevelMqttClient));
        }

        bool IsConnected => _adapter != null;

        public async Task ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            if (_adapter != null)
            {
                throw new InvalidOperationException("Low level MQTT client is already connected. Disconnect first before connecting again.");
            }

            var newAdapter = _clientAdapterFactory.CreateClientAdapter(options);

            try
            {
                _logger.Verbose("Trying to connect with server '{0}' (Timeout={1}).", options.ChannelOptions, options.CommunicationTimeout);
                await newAdapter.ConnectAsync(options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");

                _options = options;
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
            if (_adapter == null)
            {
                return;
            }

            await SafeDisconnect(cancellationToken).ConfigureAwait(false);
            _adapter = null;
        }

        public async Task SendAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is null) throw new ArgumentNullException(nameof(packet));

            if (_adapter == null)
            {
                throw new InvalidOperationException("Low level MQTT client is not connected.");
            }

            try
            {
                await _adapter.SendPacketAsync(packet, _options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await SafeDisconnect(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<MqttBasePacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_adapter == null)
            {
                throw new InvalidOperationException("Low level MQTT client is not connected.");
            }

            try
            {
                return await _adapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await SafeDisconnect(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public void Dispose()
        {
            _adapter?.Dispose();
        }

        async Task SafeDisconnect(CancellationToken cancellationToken)
        {
            try
            {
                await _adapter.DisconnectAsync(_options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting.");
            }
            finally
            {
                _adapter.Dispose();
            }
        }
    }
}
