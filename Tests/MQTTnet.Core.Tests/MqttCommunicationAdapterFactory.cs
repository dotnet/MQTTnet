using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.Tests
{
    public class MqttCommunicationAdapterFactory : IMqttCommunicationAdapterFactory
    {
        private readonly IMqttCommunicationAdapter _adapter;

        public MqttCommunicationAdapterFactory(IMqttCommunicationAdapter adapter)
        {
            _adapter = adapter;
        }

        public IMqttCommunicationAdapter CreateMqttCommunicationAdapter(MqttClientOptions options)
        {
            return _adapter;
        }
    }
}
