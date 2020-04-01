using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttClientKeepAliveMonitor
    {
        private readonly Stopwatch _lastPacketReceivedTracker = new Stopwatch();

        private readonly string _clientId;
        private readonly Func<Task> _keepAliveElapsedCallback;
        private readonly IMqttNetLogger _logger;

        private bool _isPaused;

        public MqttClientKeepAliveMonitor(string clientId, Func<Task> keepAliveElapsedCallback, IMqttNetLogger logger)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _keepAliveElapsedCallback = keepAliveElapsedCallback ?? throw new ArgumentNullException(nameof(keepAliveElapsedCallback));

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttClientKeepAliveMonitor));
        }

        public void Start(int keepAlivePeriod, CancellationToken cancellationToken)
        {
            if (keepAlivePeriod == 0)
            {
                return;
            }

            Task.Run(() => RunAsync(keepAlivePeriod, cancellationToken), cancellationToken).Forget(_logger);
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public void PacketReceived()
        {
            _lastPacketReceivedTracker.Restart();
        }

        private async Task RunAsync(int keepAlivePeriod, CancellationToken cancellationToken)
        {
            try
            {
                _lastPacketReceivedTracker.Restart();

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Values described here: [MQTT-3.1.2-24].
                    // If the client sends 5 sec. the server will allow up to 7.5 seconds.
                    // If the client sends 1 sec. the server will allow up to 1.5 seconds.
                    if (!_isPaused && _lastPacketReceivedTracker.Elapsed.TotalSeconds >= keepAlivePeriod * 1.5D)
                    {
                        _logger.Warning(null, "Client '{0}': Did not receive any packet or keep alive signal.", _clientId);
                        await _keepAliveElapsedCallback().ConfigureAwait(false);

                        return;
                    }

                    // The server checks the keep alive timeout every 50 % of the overall keep alive timeout
                    // because the server allows 1.5 times the keep alive value. This means that a value of 5 allows
                    // up to 7.5 seconds. With an interval of 2.5 (5 / 2) the 7.5 is also affected. Waiting the whole
                    // keep alive time will hit at 10 instead of 7.5 (but only one time instead of two times).
                    await Task.Delay(TimeSpan.FromSeconds(keepAlivePeriod * 0.5D), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Client '{0}': Unhandled exception while checking keep alive timeouts.", _clientId);
            }
            finally
            {
                _logger.Verbose("Client '{0}': Stopped checking keep alive timeout.", _clientId);
            }
        }
    }
}
