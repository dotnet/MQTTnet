using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server.Logging
{
    public class MqttNetLoggerWrapper : IMqttNetLogger
    {
        private readonly ILogger<MqttServer> _logger;

        public MqttNetLoggerWrapper(ILogger<MqttServer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;
        
        public IMqttNetChildLogger CreateChildLogger(string source = null)
        {
            return new MqttNetChildLoggerWrapper(source, this);
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            var convertedLogLevel = ConvertLogLevel(logLevel);
            _logger.Log(convertedLogLevel, exception, message, parameters);
        }

        private static LogLevel ConvertLogLevel(MqttNetLogLevel logLevel)
        {
            switch (logLevel)
            {
                case MqttNetLogLevel.Error: return LogLevel.Error;
                case MqttNetLogLevel.Warning: return LogLevel.Warning;
                case MqttNetLogLevel.Info: return LogLevel.Information;
                case MqttNetLogLevel.Verbose: return LogLevel.Debug;
            }

            return LogLevel.Debug;
        }
    }
}
