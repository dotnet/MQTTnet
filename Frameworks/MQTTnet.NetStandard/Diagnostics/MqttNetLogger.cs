using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogger : IMqttNetLogger
    {
        private readonly string _logId;

        public MqttNetLogger(string logId = null)
        {
            _logId = logId;
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public void Verbose<TSource>(string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Verbose, typeof(TSource), message, parameters, null);
        }

        public void Verbose(object source, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Verbose, source, message, parameters, null);
        }

        public void Info<TSource>(string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Info, typeof(TSource), message, parameters, null);
        }

        public void Info(object source, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Info, source, message, parameters, null);
        }

        public void Warning<TSource>(Exception exception, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Warning, typeof(TSource), message, parameters, null);
        }

        public void Warning(object source, Exception exception, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Warning, source, message, parameters, null);
        }

        public void Error<TSource>(Exception exception, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Error, typeof(TSource), message, parameters, null);
        }

        public void Error(object source, Exception exception, string message, params object[] parameters)
        {
            Publish(MqttNetLogLevel.Error, source, message, parameters, null);
        }

        private void Publish(MqttNetLogLevel logLevel, object source, string message, object[] parameters, Exception exception)
        {
            var hasLocalListeners = LogMessagePublished != null;
            var hasGlobalListeners = MqttNetGlobalLogger.HasListeners;

            if (!hasLocalListeners && !hasGlobalListeners)
            {
                return;
            }

            if (parameters.Length > 0)
            {
                message = string.Format(message, parameters);
            }

            string sourceName = null;
            if (source != null)
            {
                sourceName = source.GetType().Name;
            }

            var traceMessage = new MqttNetLogMessage(_logId, DateTime.Now, Environment.CurrentManagedThreadId, sourceName, logLevel, message, exception);

            if (hasGlobalListeners)
            {
                MqttNetGlobalLogger.Publish(traceMessage);
            }

            if (hasLocalListeners)
            {
                LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(traceMessage));
            }
        }
    }
}