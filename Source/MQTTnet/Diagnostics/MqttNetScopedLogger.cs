using System;

namespace MQTTnet.Diagnostics
{
    public sealed class MqttNetScopedLogger : IMqttNetScopedLogger
    {
        readonly IMqttNetLogger _logger;
        readonly string _source;

        public MqttNetScopedLogger(IMqttNetLogger logger, string source)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IMqttNetScopedLogger CreateScopedLogger(string source)
        {
            return new MqttNetScopedLogger(_logger, source);
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            _logger.Publish(logLevel, _source, message, parameters, exception);
        }
    }
}