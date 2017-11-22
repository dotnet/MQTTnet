using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientAdapterFactory
    {
        IMqttChannelAdapter CreateClientAdapter(IMqttClientChannelOptions options, IMqttNetLogger logger);
    }
}
