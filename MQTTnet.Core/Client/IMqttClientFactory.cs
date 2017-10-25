using MQTTnet.Core.ManagedClient;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();

        IManagedMqttClient CreateManagedMqttClient();
    }
}