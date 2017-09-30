using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSessionsManager
    {
        private readonly Dictionary<string, MqttClientSession> _clientSessions = new Dictionary<string, MqttClientSession>();
        private readonly MqttServerOptions _options;

        public MqttClientSessionsManager(MqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            RetainedMessagesManager = new MqttClientRetainedMessagesManager(options);
        }

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        public MqttClientRetainedMessagesManager RetainedMessagesManager { get; }

        public async Task RunClientSessionAsync(MqttClientConnectedEventArgs eventArgs)
        {
            var clientId = string.Empty;
            try
            {
                if (!(await eventArgs.ClientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false) is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                eventArgs.ClientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;
                
                var connectReturnCode = ValidateConnection(connectPacket);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await eventArgs.ClientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, CancellationToken.None, new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode
                    }).ConfigureAwait(false);

                    return;
                }

                var clientSession = GetOrCreateClientSession(connectPacket);

                await eventArgs.ClientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, CancellationToken.None, new MqttConnAckPacket
                {
                    ConnectReturnCode = connectReturnCode,
                    IsSessionPresent = clientSession.IsExistingSession
                }).ConfigureAwait(false);

                await clientSession.Session.RunAsync(eventArgs.Identifier, connectPacket.WillMessage, eventArgs.ClientAdapter).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttServer), exception, exception.Message);
            }
            finally
            {
                await eventArgs.ClientAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                ClientDisconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientId, eventArgs.ClientAdapter));
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
                    ProtocolVersion = s.Value.Adapter.PacketSerializer.ProtocolVersion
                }).ToList();
            }
        }

        public void DispatchPublishPacket(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            try
            {
                var eventArgs = new MqttApplicationMessageReceivedEventArgs(senderClientSession?.ClientId, publishPacket.ToApplicationMessage());
                ApplicationMessageReceived?.Invoke(this, eventArgs);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientSessionsManager), exception, "Error while processing application message");
            }

            lock (_clientSessions)
            {
                foreach (var clientSession in _clientSessions.Values.ToList())
                {
                    clientSession.EnqueuePublishPacket(publishPacket);
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
                        MqttTrace.Verbose(nameof(MqttClientSessionsManager), "Disposed existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        MqttTrace.Verbose(nameof(MqttClientSessionsManager), "Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this);
                    _clientSessions[connectPacket.ClientId] = clientSession;

                    MqttTrace.Verbose(nameof(MqttClientSessionsManager), "Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
        }
    }
}