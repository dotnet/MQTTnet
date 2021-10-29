using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests.Mockups
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttChannelAdapter _adapter;

        public TestMqttCommunicationAdapterFactory(IMqttChannelAdapter adapter)
        {
            _adapter = adapter;
        }

        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetLogger logger)
        {
            return _adapter;
        }
    }
}
