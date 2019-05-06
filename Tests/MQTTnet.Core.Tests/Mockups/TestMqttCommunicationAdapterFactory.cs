using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests.Mockups
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        private readonly IMqttChannelAdapter _adapter;

        public TestMqttCommunicationAdapterFactory(IMqttChannelAdapter adapter)
        {
            _adapter = adapter;
        }
        
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            return _adapter;
        }
    }
}
