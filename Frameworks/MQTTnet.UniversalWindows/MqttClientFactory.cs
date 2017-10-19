using MQTTnet.Core.Client;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttClientFactory : IMqttClientFactory
    {
        public IMqttClient CreateMqttClient()
        {
            return new MqttClient(new MqttCommunicationAdapterFactory());
        }

        public ManagedMqttClient CreateManagedMqttClient()
        {
            return new ManagedMqttClient(new MqttCommunicationAdapterFactory());
        }
    }
}