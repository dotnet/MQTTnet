using System;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Tests
{
    public class TestClientSessionFactory : IMqttClientSesssionFactory
    {
        public MqttClientSession CreateClientSession(string clientId, MqttClientSessionsManager mqttClientSessionsManager)
        {
            throw new NotImplementedException();
            //return new MqttClientSession(clientId, mqttClientSessionsManager, new TestLogger<MqttClientSession>(), new TestLogger<MqttClientPendingMessagesQueue>());
        }
    }
}
