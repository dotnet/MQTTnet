using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager
    {
        private readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IMqttServerOptions _options;
        private readonly MqttServer _server;
        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttNetLogger _logger;

        public MqttClientSessionsManager(IMqttServerOptions options, MqttRetainedMessagesManager retainedMessagesManager, MqttServer server, IMqttNetLogger logger)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

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

                var clientSession = await GetOrCreateClientSessionAsync(connectPacket).ConfigureAwait(false);

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttConnAckPacket
                {
                    ConnectReturnCode = connectReturnCode,
                    IsSessionPresent = clientSession.IsExistingSession
                }).ConfigureAwait(false);

                _server.OnClientConnected(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                });

                await clientSession.Session.RunAsync(connectPacket, clientAdapter).ConfigureAwait(false);
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

                _server.OnClientDisconnected(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                });
            }
        }

        public async Task StopAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var session in _sessions)
                {
                    await session.Value.StopAsync().ConfigureAwait(false);
                }

                _sessions.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _sessions.Where(s => s.Value.IsConnected).Select(s => new ConnectedMqttClient
                {
                    ClientId = s.Value.ClientId,
                    ProtocolVersion = s.Value.ProtocolVersion ?? MqttProtocolVersion.V311,
                    LastPacketReceived = s.Value.LastPacketReceived,
                    LastNonKeepAlivePacketReceived = s.Value.LastNonKeepAlivePacketReceived
                }).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DispatchApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                applicationMessage = InterceptApplicationMessage(applicationMessage);
                if (applicationMessage == null)
                {
                    return;
                }

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientSession?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                _server.OnApplicationMessageReceived(senderClientSession?.ClientId, applicationMessage);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, "Error while processing application message");
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var clientSession in _sessions.Values)
                {
                    await clientSession.EnqueueApplicationMessageAsync(applicationMessage);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private MqttApplicationMessage InterceptApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            if (_options.ApplicationMessageInterceptor == null)
            {
                return applicationMessage;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext
            {
                ApplicationMessage = applicationMessage
            };

            _options.ApplicationMessageInterceptor(interceptorContext);
            return interceptorContext.ApplicationMessage;
        }

        private MqttConnectReturnCode ValidateConnection(MqttConnectPacket connectPacket)
        {
            if (_options.ConnectionValidator == null)
            {
                return MqttConnectReturnCode.ConnectionAccepted;
            }
            
            var context = new MqttConnectionValidatorContext(
                connectPacket.ClientId,
                connectPacket.Username,
                connectPacket.Password,
                connectPacket.WillMessage);

            _options.ConnectionValidator(context);
            return context.ReturnCode;
        }

        private async Task<GetOrCreateClientSessionResult> GetOrCreateClientSessionAsync(MqttConnectPacket connectPacket)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {   
                        _sessions.Remove(connectPacket.ClientId);

                        await clientSession.StopAsync().ConfigureAwait(false);
                        clientSession.Dispose();
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

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, _retainedMessagesManager, this, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Trace<MqttClientSessionsManager>("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}