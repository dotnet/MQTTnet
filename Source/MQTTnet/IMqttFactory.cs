using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet
{
    public interface IMqttFactory : IMqttClientFactory, IMqttServerFactory
    {
        IMqttNetLogger DefaultLogger { get; }
    }
}
