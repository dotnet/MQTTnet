namespace MQTTnet.Core.Server
{
    public interface IMqttClientSesssionFactory
    {
        MqttClientSession CreateClientSession(string sessionId, MqttClientSessionsManager mqttClientSessionsManager);
    }
}
