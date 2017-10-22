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
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSessionsManager
    {
        private readonly Dictionary<string, MqttClientSession> _clientSessions = new Dictionary<string, MqttClientSession>();
        private readonly MqttServerOptions _options;
        private readonly MqttNetTrace _trace;

        public MqttClientSessionsManager(MqttServerOptions options, MqttNetTrace trace)
        {
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            RetainedMessagesManager = new MqttClientRetainedMessagesManager(options, trace);
        }

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;

        public MqttClientRetainedMessagesManager RetainedMessagesManager { get; }

        public async Task RunClientSessionAsync(IMqttCommunicationAdapter clientAdapter)
        {
            var clientId = string.Empty;
            try
            {
                if (!(await clientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false) is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                clientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;
                
                var connectReturnCode = ValidateConnection(connectPacket);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, CancellationToken.None, new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode
                    }).ConfigureAwait(false);

                    return;
                }

                var clientSession = GetOrCreateClientSession(connectPacket);

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, CancellationToken.None, new MqttConnAckPacket
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
                _trace.Error(nameof(MqttServer), exception, exception.Message);
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
                _trace.Error(nameof(MqttClientSessionsManager), exception, "Error while processing application message");
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
                        _trace.Verbose(nameof(MqttClientSessionsManager), "Disposed existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _trace.Verbose(nameof(MqttClientSessionsManager), "Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _trace);
                    _clientSessions[connectPacket.ClientId] = clientSession;

                    _trace.Verbose(nameof(MqttClientSessionsManager), "Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
        }
    }
}