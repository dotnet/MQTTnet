﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Serializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSessionsManager
    {
        private readonly Dictionary<string, MqttClientSession> _clientSessions = new Dictionary<string, MqttClientSession>();
        private readonly ILogger<MqttClientSessionsManager> _logger;
        private readonly IMqttClientSesssionFactory _clientSesssionFactory;
        private readonly MqttServerOptions _options;

        public MqttClientSessionsManager(
            IOptions<MqttServerOptions> options, 
            ILogger<MqttClientSessionsManager> logger,
            IMqttClientSesssionFactory clientSesssionFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _clientSesssionFactory = clientSesssionFactory ?? throw new ArgumentNullException(nameof(clientSesssionFactory));
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public async Task RunClientSessionAsync(IMqttCommunicationAdapter clientAdapter, CancellationToken cancellationToken)
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

                var clientSession = GetOrCreateClientSession(connectPacket);

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

                using (_logger.BeginScope(clientId))
                {
                    await clientSession.Session.RunAsync(connectPacket.WillMessage, clientAdapter).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, exception.Message);
            }
            finally
            {
                try
                {
                    await clientAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    //ignored
                }

                ClientDisconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                }));
            }
        }

        public void Clear()
        {
            lock (_clientSessions)
            {
                _clientSessions.Clear();
            }
        }

        public IList<ConnectedMqttClient> GetConnectedClients()
        {
            lock (_clientSessions)
            {
                return _clientSessions.Where(s => s.Value.IsConnected).Select(s => new ConnectedMqttClient
                {
                    ClientId = s.Value.ClientId,
                    ProtocolVersion = s.Value.ProtocolVersion ?? MqttProtocolVersion.V311
                }).ToList();
            }
        }

        public void DispatchApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                var eventArgs = new MqttApplicationMessageReceivedEventArgs(senderClientSession?.ClientId, applicationMessage);
                ApplicationMessageReceived?.Invoke(this, eventArgs);
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Error while processing application message");
            }

            lock (_clientSessions)
            {
                foreach (var clientSession in _clientSessions.Values.ToList())
                {
                    clientSession.EnqueuePublishPacket(applicationMessage.ToPublishPacket());
                }
            }
        }

        private MqttConnectReturnCode ValidateConnection(MqttConnectPacket connectPacket)
        {
            if (_options.ConnectionValidator != null)
            {
                return _options.ConnectionValidator(connectPacket);
            }

            return MqttConnectReturnCode.ConnectionAccepted;
        }

        private GetOrCreateClientSessionResult GetOrCreateClientSession(MqttConnectPacket connectPacket)
        {
            lock (_clientSessions)
            {
                var isSessionPresent = _clientSessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _clientSessions.Remove(connectPacket.ClientId);
                        clientSession.Dispose();
                        clientSession = null;

                        _logger.LogTrace("Disposed existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.LogTrace("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = _clientSesssionFactory.CreateClientSession(connectPacket.ClientId, this);
                    _clientSessions[connectPacket.ClientId] = clientSession;

                    _logger.LogTrace("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
        }
    }
}