using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public class MqttClientSessionManager
    {
        private readonly ConcurrentDictionary<string, MqttClientSession> _clientSessions = new ConcurrentDictionary<string, MqttClientSession>();
        private readonly MqttServerOptions _options;

        public MqttClientSessionManager(MqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunClientSessionAsync(MqttClientConnectedEventArgs eventArgs)
        {
            try
            {
                var connectPacket = await eventArgs.ClientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout) as MqttConnectPacket;
                if (connectPacket == null)
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'Connect' packet [MQTT-3.1.0-1].");
                }

                var connectReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                if (_options.ConnectionValidator != null)
                {
                    connectReturnCode = _options.ConnectionValidator(connectPacket);
                }

                MqttClientSession clientSession = null;
                var isSessionPresent = _clientSessions.ContainsKey(connectPacket.ClientId);
                if (isSessionPresent && connectPacket.CleanSession)
                {
                    MqttClientSession _;
                    _clientSessions.TryRemove(connectPacket.ClientId, out _);
                }
                else if (!connectPacket.CleanSession)
                {
                    _clientSessions.TryGetValue(connectPacket.ClientId, out clientSession);
                }

                await eventArgs.ClientAdapter.SendPacketAsync(new MqttConnAckPacket
                {
                    ConnectReturnCode = connectReturnCode,
                    IsSessionPresent = clientSession != null
                }, _options.DefaultCommunicationTimeout);

                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    return;
                }

                if (clientSession == null)
                {
                    clientSession = new MqttClientSession(_options, DispatchPublishPacket);
                    _clientSessions.TryAdd(connectPacket.ClientId, clientSession);
                }

                await clientSession.RunAsync(eventArgs.Identifier, connectPacket.WillMessage, eventArgs.ClientAdapter);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttServer), exception, exception.Message);
            }
            finally
            {
                await eventArgs.ClientAdapter.DisconnectAsync();
            }
        }

        public void Clear()
        {
            _clientSessions.Clear();
        }

        private void DispatchPublishPacket(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            foreach (var clientSession in _clientSessions.Values.ToList())
            {
                clientSession.DispatchPublishPacket(senderClientSession, publishPacket);
            }
        }
    }
}
