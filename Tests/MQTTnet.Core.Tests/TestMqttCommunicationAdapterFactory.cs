using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Tests
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        private readonly IMqttChannelAdapter _adapter;

        public TestMqttCommunicationAdapterFactory(IMqttChannelAdapter adapter)
        {
            _adapter = adapter;
        }
        
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientChannelOptions options, IMqttNetLogger logger)
        {
            return _adapter;
        }
    }
}
