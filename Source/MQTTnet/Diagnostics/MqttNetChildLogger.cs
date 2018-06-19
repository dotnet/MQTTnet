using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetChildLogger : IMqttNetChildLogger
    {
        private readonly IMqttNetLogger _logger;
        private readonly string _source;

        public MqttNetChildLogger(IMqttNetLogger logger, string source)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source;
        }

        public IMqttNetChildLogger CreateChildLogger(string source)
        {
            string childSource;
            if (!string.IsNullOrEmpty(_source))
            {
                childSource = _source + "." + source;
            }
            else
            {
                childSource = source;
            }

            return new MqttNetChildLogger(_logger, childSource);
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
