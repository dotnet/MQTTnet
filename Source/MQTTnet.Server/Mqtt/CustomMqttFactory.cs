using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server.Logging;

namespace MQTTnet.Server.Mqtt
{
    public class CustomMqttFactory
    {
        private readonly MqttFactory _mqttFactory;

        public CustomMqttFactory(ILogger<MqttServer> logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            Logger = new MqttNetLoggerWrapper(logger);

            _mqttFactory = new MqttFactory(Logger);
        }
        
        public IMqttNetLogger Logger { get; }

        public IMqttServer CreateMqttServer(List<IMqttServerAdapter> adapters)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));

            return _mqttFactory.CreateMqttServer(adapters);
        }
    }
}
