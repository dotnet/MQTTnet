using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogger : IMqttNetLogger
    {
        readonly string _logId;
        
        public MqttNetLogger()
        {
        }

        public MqttNetLogger(string logId)
        {
            _logId = logId;
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        // TODO: Consider creating a LoggerFactory which will allow creating loggers. The logger factory will
        // be the only place which has the published event.
        public IMqttNetScopedLogger CreateScopedLogger(string source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new MqttNetScopedLogger(this, source);
        }

        public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
        {
            var hasLocalListeners = LogMessagePublished != null;
            var hasGlobalListeners = MqttNetGlobalLogger.HasListeners;

            if (!hasLocalListeners && !hasGlobalListeners)
            {
                return;
            }

            if (parameters?.Length > 0 && message?.Length > 0)
            {
                try
                {
                    message = string.Format(message, parameters);
                }
                catch (FormatException)
                {
                    message = "MESSAGE FORMAT INVALID: " + message;
                }
            }

            var logMessage = new MqttNetLogMessage
            {
                LogId = _logId,
                Timestamp = DateTime.UtcNow,
                Source = source,
                ThreadId = Environment.CurrentManagedThreadId,
                Level = level,
                Message = message,
                Exception = exception
            };

            if (hasGlobalListeners)
            {
                MqttNetGlobalLogger.Publish(logMessage);
            }

            if (hasLocalListeners)
            {
                LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(logMessage));
            }
        }
    }
}