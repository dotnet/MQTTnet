using System.Collections.Generic;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Server
{
    public interface IMqttServerFactory
    {
        IMqttServer CreateMqttServer();

        IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger);
    }
}