using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Core.Tests
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        private readonly IMqttChannelAdapter _adapter;

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
