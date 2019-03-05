using System;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server.Logging
{
    public class MqttNetChildLoggerWrapper : IMqttNetChildLogger
    {
        private readonly MqttNetLoggerWrapper _logger;
        private readonly string _source;

        public MqttNetChildLoggerWrapper(string source, MqttNetLoggerWrapper logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _source = source;
        }

        public IMqttNetChildLogger CreateChildLogger(string source = null)
        {
            return _logger.CreateChildLogger(source);
        }

        public void Verbose(string message, params object[] parameters)
        {
            _logger.Publish(MqttNetLogLevel.Verbose, _source, message, parameters, null);
        }

        public void Info(string message, params object[] parameters)
        {
            _logger.Publish(MqttNetLogLevel.Info, _source, message, parameters, null);
        }

        public void Warning(Exception exception, string message, params object[] parameters)
        {
            _logger.Publish(MqttNetLogLevel.Warning, _source, message, parameters, exception);
        }

        public void Error(Exception exception, string message, params object[] parameters)
        {
            _logger.Publish(MqttNetLogLevel.Error, _source, message, parameters, exception);
        }
    }
}
