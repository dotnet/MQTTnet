using System;

namespace MQTTnet.Diagnostics
{
    public static class MqttNetLoggerExtensions
    {
        public static void Verbose(this IMqttNetLogger logger, string message, params object[] parameters)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            logger.Publish(MqttNetLogLevel.Verbose, message, parameters, null);
        }

        public static void Info(this IMqttNetLogger logger, string message, params object[] parameters)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            logger.Publish(MqttNetLogLevel.Info, message, parameters, null);
        }

        public static void Warning(this IMqttNetLogger logger, Exception exception, string message, params object[] parameters)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            logger.Publish(MqttNetLogLevel.Warning, message, parameters, exception);
        }

        public static void Error(this IMqttNetLogger logger, Exception exception, string message, params object[] parameters)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            logger.Publish(MqttNetLogLevel.Error, message, parameters, exception);
        }
    }
}
