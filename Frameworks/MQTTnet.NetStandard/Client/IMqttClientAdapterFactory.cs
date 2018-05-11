using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Client
{
    public interface IMqttClientAdapterFactory
    {
        IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger);
    }
}
