using MQTTnet.Client.Options;

namespace MQTTnet.Adapter
{
    public interface IMqttClientAdapterFactory
    {
        IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options);
    }
}
