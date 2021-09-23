using System;

namespace MQTTnet.Diagnostics
{
    public static class MqttNetSourceLoggerExtensions
    {
        public static MqttNetSourceLogger WithSource(this IMqttNetLogger logger, string source)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            return new MqttNetSourceLogger(logger, source);
        }
        
        public static void Verbose(this MqttNetSourceLogger logger, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Verbose, message, parameters, null);
        }
        
        public static void Verbose(this MqttNetSourceLogger logger, string message)
        {
            logger.Publish(MqttNetLogLevel.Verbose, message, null, null);
        }

        public static void Info(this MqttNetSourceLogger logger, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Info, message, parameters, null);
        }
        
        public static void Info(this MqttNetSourceLogger logger, string message)
        {
            logger.Publish(MqttNetLogLevel.Info, message, null, null);
        }

        public static void Warning(this MqttNetSourceLogger logger, Exception exception, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Warning, message, parameters, exception);
        }
        
        public static void Warning(this MqttNetSourceLogger logger, Exception exception, string message)
        {
            logger.Publish(MqttNetLogLevel.Warning, message, null, exception);
        }
        
        public static void Warning(this MqttNetSourceLogger logger, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Warning, message, parameters, null);
        }
        
        public static void Warning(this MqttNetSourceLogger logger, string message)
        {
            logger.Publish(MqttNetLogLevel.Warning, message, null, null);
        }

        public static void Error(this MqttNetSourceLogger logger, Exception exception, string message, params object[] parameters)
        {
            logger.Publish(MqttNetLogLevel.Error, message, parameters, exception);
        }
        
        public static void Error(this MqttNetSourceLogger logger, Exception exception, string message)
        {
            logger.Publish(MqttNetLogLevel.Error, message, null, exception);
        }
        
        public static void Error(this MqttNetSourceLogger logger, string message)
        {
            logger.Publish(MqttNetLogLevel.Error, message, null, null);
        }
    }
}
