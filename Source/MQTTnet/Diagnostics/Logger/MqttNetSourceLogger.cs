using System;

namespace MQTTnet.Diagnostics.Logger
{
    public sealed class MqttNetSourceLogger
    {
        readonly IMqttNetLogger _logger;
        readonly string _source;

        public MqttNetSourceLogger(IMqttNetLogger logger, string source)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source;
        }

        public bool IsEnabled => _logger.IsEnabled;
        
        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            _logger.Publish(logLevel, _source, message, parameters, exception);
        }
    }
}