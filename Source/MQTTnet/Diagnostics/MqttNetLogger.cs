using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogger : IMqttNetLogger
    {
        readonly string _logId;
        readonly string _source;

        readonly MqttNetLogger _parentLogger;

        public MqttNetLogger(string source, string logId)
        {
            _source = source;
            _logId = logId;
        }

        public MqttNetLogger()
        {
        }

        public MqttNetLogger(string logId)
        {
            _logId = logId;
        }

        MqttNetLogger(MqttNetLogger parentLogger, string logId, string source)
        {
            _parentLogger = parentLogger ?? throw new ArgumentNullException(nameof(parentLogger));
            _source = source ?? throw new ArgumentNullException(nameof(source));

            _logId = logId;
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        // TODO: Consider creating a LoggerFactory which will allow creating loggers. The logger factory will
        // be the only place which has the published event.
        public IMqttNetLogger CreateChildLogger(string source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new MqttNetLogger(this, _logId, source);
        }

        public void Publish(MqttNetLogLevel level, string message, object[] parameters, Exception exception)
        {
            var hasLocalListeners = LogMessagePublished != null;
            var hasGlobalListeners = MqttNetGlobalLogger.HasListeners;

            if (!hasLocalListeners && !hasGlobalListeners && _parentLogger == null)
            {
                return;
            }

            if (parameters?.Length > 0)
            {
                try
                {
                    message = string.Format(message, parameters);
                }
                catch
                {
                    message = "MESSAGE FORMAT INVALID: " + message;
                }
            }

            var logMessage = new MqttNetLogMessage
            {
                LogId = _logId,
                Timestamp = DateTime.UtcNow,
                Source = _source,
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

            _parentLogger?.Publish(logMessage);
        }

        void Publish(MqttNetLogMessage logMessage)
        {
            LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(logMessage));

            _parentLogger?.Publish(logMessage);
        }
    }
}