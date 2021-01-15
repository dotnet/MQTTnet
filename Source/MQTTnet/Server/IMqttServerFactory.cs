using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public interface IMqttServerFactory
    {
        IList<Func<IMqttFactory, IMqttServerAdapter>> DefaultServerAdapters { get; }

        IMqttServer CreateMqttServer();

        IMqttServer CreateMqttServer(IMqttNetLogger logger);

        IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters);

        IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger);
    }
}