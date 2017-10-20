using System;
using System.Collections.Generic;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttServerFactory : IMqttServerFactory
    {
        public IMqttServer CreateMqttServer(MqttServerOptions options, IMqttNetTraceHandler traceHandler = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var trace = new MqttNetTrace(traceHandler);
            return new MqttServer(options, new List<IMqttServerAdapter> { new MqttServerAdapter(trace) }, trace);
        }
    }
}
