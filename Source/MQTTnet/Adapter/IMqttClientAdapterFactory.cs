using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Adapter
{
    public interface IMqttClientAdapterFactory
    {
        IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttPacketInspectorHandler packetInspectorHandler, IMqttNetLogger logger);
    }
}
