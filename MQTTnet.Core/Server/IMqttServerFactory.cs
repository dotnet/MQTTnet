namespace MQTTnet.Core.Server
{
    public interface IMqttServerFactory
    {
        IMqttServer CreateMqttServer(MqttServerOptions options);
    }
}