using System;

namespace MQTTnet.Diagnostics
{
    public static class MqttNetScopedLoggerExtensions
    {
        public static void Verbose(this IMqttNetScopedLogger logger, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Verbose, message, parameters, null);
        }

        public static void Info(this IMqttNetScopedLogger logger, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Info, message, parameters, null);
        }

        public static void Warning(this IMqttNetScopedLogger logger, Exception exception, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Warning, message, parameters, exception);
        }

        public static void Error(this IMqttNetScopedLogger logger, Exception exception, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Error, message, parameters, exception);
        }
    }
}
