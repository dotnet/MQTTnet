using MQTTnet.Adapter;
using MQTTnet.Client.Options;

namespace MQTTnet.Tests.Mockups
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttChannelAdapter _adapter;

        public TestMqttCommunicationAdapterFactory(IMqttChannelAdapter adapter)
        {
            _adapter = adapter;
        }

        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options)
        {
            return _adapter;
        }
    }
}
