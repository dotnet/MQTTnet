using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Implementations;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttServerKeepAliveMonitor
    {
        readonly IMqttServerOptions _options;
        readonly MqttClientSessionsManager _sessionsManager;
        readonly IMqttNetScopedLogger _logger;

        public MqttServerKeepAliveMonitor(IMqttServerOptions options, MqttClientSessionsManager sessionsManager, IMqttNetLogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttServerKeepAliveMonitor));
        }

        public void Start(CancellationToken cancellationToken)
        {
            // The keep alive monitor spawns a real new thread (LongRunning) because it does not 
            // support async/await. Async etc. is avoided here because the thread will usually check
            // the connections every few milliseconds and thus the context changes (due to async) are 
            // only consuming resources. Also there is just 1 thread for the entire server which is fine at all!
            Task.Factory.StartNew(_ => DoWork(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning).Forget(_logger);
        }

        void DoWork(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Starting keep alive monitor.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    TryMaintainConnections();
                    PlatformAbstractionLayer.Sleep(_options.KeepAliveMonitorInterval);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while checking keep alive timeouts.");
            }
            finally
            {
                _logger.Verbose("Stopped checking keep alive timeout.");
            }
        }

        void TryMaintainConnections()
        {
            var now = DateTime.UtcNow;
            foreach (var connection in _sessionsManager.GetConnections())
            {
                TryMaintainConnection(connection, now);
            }
        }

        void TryMaintainConnection(MqttClientConnection connection, DateTime now)
        {
            try
            {
                if (connection.Status != MqttClientConnectionStatus.Running)
                {
                    // The connection is already dead or just created so there is no need to check it.
                    return;
                }

                if (connection.ConnectPacket.KeepAlivePeriod == 0)
                {
                    // The keep alive feature is not used by the current connection.
                    return;
                }

                if (connection.IsReadingPacket)
                {
                    // The connection is currently reading a (large) packet. So it is obviously 
                    // doing something and thus "connected".
                    return;
                }

                // Values described here: [MQTT-3.1.2-24].
                // If the client sends 5 sec. the server will allow up to 7.5 seconds.
                // If the client sends 1 sec. the server will allow up to 1.5 seconds.
                var maxDurationWithoutPacket = connection.ConnectPacket.KeepAlivePeriod * 1.5D;

                var secondsWithoutPackage = (now - connection.LastPacketReceivedTimestamp).TotalSeconds;
                if (secondsWithoutPackage < maxDurationWithoutPacket)
                {
                    // A packet was received before the timeout is affected.
                    return;
                }

                _logger.Warning(null, "Client '{0}': Did not receive any packet or keep alive signal.", connection.ClientId);

                // Execute the disconnection in background so that the keep alive monitor can continue
                // with checking other connections.
                // We do not need to wait for the task so no await is needed.
                // Also the internal state of the connection must be swapped to "Finalizing" because the
                // next iteration of the keep alive timer happens.
                var _ = connection.StopAsync(MqttDisconnectReasonCode.KeepAliveTimeout);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Client {0}: Unhandled exception while checking keep alive timeouts.", connection.ClientId);
            }
        }
    }
}
