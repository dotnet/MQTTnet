using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class MqttClientKeepAliveMonitor
    {
        private readonly Stopwatch _lastPacketReceivedTracker = new Stopwatch();
        private readonly Stopwatch _lastNonKeepAlivePacketReceivedTracker = new Stopwatch();

        private readonly string _clientId;
        private readonly Func<Task> _timeoutCallback;
        private readonly IMqttNetLogger _logger;

        public MqttClientKeepAliveMonitor(string clientId, Func<Task> timeoutCallback, IMqttNetLogger logger)
        {
            _clientId = clientId;
            _timeoutCallback = timeoutCallback;
            _logger = logger;
        }

        public TimeSpan LastPacketReceived => _lastPacketReceivedTracker.Elapsed;

        public TimeSpan LastNonKeepAlivePacketReceived => _lastNonKeepAlivePacketReceivedTracker.Elapsed;

        public void Start(int keepAlivePeriod, CancellationToken cancellationToken)
        {
            if (keepAlivePeriod == 0)
            {
                return;
            }

            Task.Run(async () => await RunAsync(keepAlivePeriod, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task RunAsync(int keepAlivePeriod, CancellationToken cancellationToken)
        {
            try
            {
                _lastPacketReceivedTracker.Restart();
                _lastNonKeepAlivePacketReceivedTracker.Restart();

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Values described here: [MQTT-3.1.2-24].
                    if (_lastPacketReceivedTracker.Elapsed.TotalSeconds > keepAlivePeriod * 1.5D)
                    {
                        _logger.Warning<MqttClientSession>("Client '{0}': Did not receive any packet or keep alive signal.", _clientId);

                        if (_timeoutCallback != null)
                        {
                            await _timeoutCallback().ConfigureAwait(false);
                        }

                        return;
                    }

                    await Task.Delay(keepAlivePeriod, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSession>(exception, "Client '{0}': Unhandled exception while checking keep alive timeouts.", _clientId);
            }
            finally
            {
                _logger.Trace<MqttClientSession>("Client {0}: Stopped checking keep alive timeout.", _clientId);
            }
        }

        public void PacketReceived(MqttBasePacket packet)
        {
            _lastPacketReceivedTracker.Restart();

            if (!(packet is MqttPingReqPacket))
            {
                _lastNonKeepAlivePacketReceivedTracker.Restart();
            }
        }
    }
}
