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
            Publish<TSource>(MqttNetLogLevel.Verbose, null, message, parameters);
        }

        public void Info<TSource>(string message, params object[] parameters)
        {
            Publish<TSource>(MqttNetLogLevel.Info, null, message, parameters);
        }

        public void Warning<TSource>(Exception exception, string message, params object[] parameters)
        {
            Publish<TSource>(MqttNetLogLevel.Warning, exception, message, parameters);
        }

        public void Warning<TSource>(string message, params object[] parameters)
        {
            Warning<TSource>(null, message, parameters);
        }

        public void Error<TSource>(Exception exception, string message, params object[] parameters)
        {
            Publish<TSource>(MqttNetLogLevel.Error, exception, message, parameters);
        }

        public void Error<TSource>(string message, params object[] parameters)
        {
            Warning<TSource>(null, message, parameters);
        }

        private void Publish<TSource>(MqttNetLogLevel logLevel, Exception exception, string message, object[] parameters)
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

            var traceMessage = new MqttNetLogMessage(_logId, DateTime.Now, Environment.CurrentManagedThreadId, typeof(TSource).Name, logLevel, message, exception);

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