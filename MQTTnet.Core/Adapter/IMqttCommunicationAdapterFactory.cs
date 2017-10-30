using MQTTnet.Core.Client;
using MQTTnet.Core.Channel;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttCommunicationAdapterFactory
    {
        IMqttCommunicationAdapter CreateClientCommunicationAdapter(IMqttClientOptions options);

        IMqttCommunicationAdapter CreateServerCommunicationAdapter(IMqttCommunicationChannel channel);
    }
}