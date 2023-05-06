// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;

namespace MQTTnet.Client.Internal
{
    public sealed class MqttClientKeepAliveHandler
    {
        readonly MqttClient _client;
        readonly MqttNetSourceLogger _logger;

        CancellationTokenSource _cancellationToken;
        TimeSpan _keepAliveInterval;
        Task _keepAlivePacketsSenderTask;
        DateTime _lastPacketSentTimestamp;

        public MqttClientKeepAliveHandler(MqttClient client, IMqttNetLogger logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger.WithSource(nameof(MqttClientKeepAliveHandler));
        }

        public void Disable()
        {
            try
            {
                _cancellationToken?.Cancel(false);
            }
            finally
            {
                _cancellationToken?.Dispose();
                _cancellationToken = null;
            }
        }

        public void Enable(MqttClientConnectResult connectResult)
        {
            if (connectResult == null)
            {
                throw new ArgumentNullException(nameof(connectResult));
            }

            _keepAliveInterval = _client.Options.KeepAlivePeriod;

            if (connectResult.ServerKeepAlive > 0)
            {
                _logger.Info("Using keep alive value {0} sent from the server", connectResult.ServerKeepAlive);
                _keepAliveInterval = TimeSpan.FromSeconds(connectResult.ServerKeepAlive);
            }

            if (_keepAliveInterval == TimeSpan.Zero)
            {
                return;
            }

            _cancellationToken = new CancellationTokenSource();
            _keepAlivePacketsSenderTask = Task.Run(() => SendKeepAliveMessagesLoop(_cancellationToken.Token), _cancellationToken.Token);
        }

        public void TrackSentPacket()
        {
            // Only use UTC to avoid performance hit by loading current time zone all the time.
            _lastPacketSentTimestamp = DateTime.UtcNow;
        }

        async Task SendKeepAliveMessagesLoop(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start sending keep alive packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Values described here: [MQTT-3.1.2-24].
                    var timeWithoutPacketSent = DateTime.UtcNow - _lastPacketSentTimestamp;

                    if (timeWithoutPacketSent > _keepAliveInterval)
                    {
                        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                        {
                            timeout.CancelAfter(_client.Options.Timeout);
                            await _client.PingAsync(timeout.Token).ConfigureAwait(false);
                        }
                    }

                    // Wait a fixed time in all cases. Calculation of the remaining time is complicated
                    // due to some edge cases and was buggy in the past. Now we wait several ms because the
                    // min keep alive value is one second so that the server will wait 1.5 seconds for a PING packet.
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                if (_client._cleanDisconnectInitiated)
                {
                    return;
                }

                if (exception is OperationCanceledException)
                {
                    return;
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Communication error while sending/receiving keep alive packets");
                }
                else
                {
                    _logger.Error(exception, "Error exception while sending/receiving keep alive packets");
                }

                await _client.DisconnectInternalAsync(_keepAlivePacketsSenderTask, exception, null).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose("Stopped sending keep alive packets");
            }
        }
    }
}