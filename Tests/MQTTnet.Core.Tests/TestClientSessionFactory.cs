using MQTTnet.Core.Server;

namespace MQTTnet.Core.Tests
{
    public class TestClientSessionFactory : IMqttClientSesssionFactory
    {
        public MqttClientSession CreateClientSession(string sessionId, MqttClientSessionsManager mqttClientSessionsManager)
        {
            return new MqttClientSession(sessionId, mqttClientSessionsManager, new TestLogger<MqttClientSession>(), new TestLogger<MqttClientPendingMessagesQueue>());
        }
    }
}
