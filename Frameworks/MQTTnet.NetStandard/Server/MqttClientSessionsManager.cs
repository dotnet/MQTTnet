using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager
    {
        private readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();
        private readonly SemaphoreSlim _sessionsSemaphore = new SemaphoreSlim(1, 1);

        private readonly MqttServerOptions _options;
        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttNetLogger _logger;

        public MqttClientSessionsManager(MqttServerOptions options, MqttRetainedMessagesManager retainedMessagesManager, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public async Task RunClientSessionAsync(IMqttChannelAdapter clientAdapter, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;
            try
            {
                if (!(await clientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false) is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                clientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;

                var connectReturnCode = ValidateConnection(connectPacket);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode
                    }).ConfigureAwait(false);

                    return;
                }

                var clientSession = await GetOrCreateClientSessionAsync(connectPacket);

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttConnAckPacket
                {
                    ConnectReturnCode = connectReturnCode,
                    IsSessionPresent = clientSession.IsExistingSession
                }).ConfigureAwait(false);

                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                }));

                await clientSession.Session.RunAsync(connectPacket.WillMessage, clientAdapter).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, exception.Message);
            }
            finally
            {
                try
                {
                    await clientAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                }

                ClientDisconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                }));
            }
        }

        public async Task StopAsync()
        {
            await _sessionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var session in _sessions)
                {
                    await session.Value.StopAsync();
                }

                _sessions.Clear();
            }
            finally
            {
                _sessionsSemaphore.Release();
            }
        }

        public async Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            await _sessionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _sessions.Where(s => s.Value.IsConnected).Select(s => new ConnectedMqttClient
                {
                    ClientId = s.Value.ClientId,
                    ProtocolVersion = s.Value.ProtocolVersion ?? MqttProtocolVersion.V311
                }).ToList();
            }
            finally
            {
                _sessionsSemaphore.Release();
            }
        }

        public async Task DispatchApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                var interceptorContext = new MqttApplicationMessageInterceptorContext
                {
                    ApplicationMessage = applicationMessage
                };

                _options.ApplicationMessageInterceptor?.Invoke(interceptorContext);
                applicationMessage = interceptorContext.ApplicationMessage;

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientSession?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                var eventArgs = new MqttApplicationMessageReceivedEventArgs(senderClientSession?.ClientId, applicationMessage);
                ApplicationMessageReceived?.Invoke(this, eventArgs);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, "Error while processing application message");
            }

            lock (_sessions)
            {
                foreach (var clientSession in _sessions.Values.ToList())
                {
                    clientSession.EnqueuePublishPacket(applicationMessage.ToPublishPacket());
                }
            }
        }

        public Task<List<MqttApplicationMessage>> GetRetainedMessagesAsync(MqttSubscribePacket subscribePacket)
        {
            return _retainedMessagesManager.GetSubscribedMessagesAsync(subscribePacket);
        }

        private MqttConnectReturnCode ValidateConnection(MqttConnectPacket connectPacket)
        {
            if (_options.ConnectionValidator != null)
            {
                return _options.ConnectionValidator(connectPacket);
            }

            return MqttConnectReturnCode.ConnectionAccepted;
        }

        private async Task<GetOrCreateClientSessionResult> GetOrCreateClientSessionAsync(MqttConnectPacket connectPacket)
        {
            await _sessionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _sessions.Remove(connectPacket.ClientId);
                        await clientSession.StopAsync();
                        clientSession = null;

                        _logger.Trace<MqttClientSessionsManager>("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Trace<MqttClientSessionsManager>("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Trace<MqttClientSessionsManager>("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
            finally
            {
                _sessionsSemaphore.Release();
            }
        }
    }
}