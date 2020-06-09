using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet
{
    public interface IMqttFactory : IMqttClientFactory, IMqttServerFactory
    {
        IMqttNetLogger DefaultLogger { get; }

        IDictionary<object, object> Properties { get; }
    }
}
