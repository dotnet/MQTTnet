using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogger : IMqttNetLogger
    {
        readonly string _logId;
        readonly string _source;

        public MqttNetLogger(string source, string logId = null)
        {
            _source = source;
            _logId = logId;
        }

        public MqttNetLogger()
        {
        }

        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public IMqttNetLogger CreateChildLogger(string source = null)
        {
            return new MqttNetLogger(source, _logId);
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            var hasLocalListeners = LogMessagePublished != null;
            var hasGlobalListeners = MqttNetGlobalLogger.HasListeners;

            if (!hasLocalListeners && !hasGlobalListeners)
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

            var traceMessage = new MqttNetLogMessage(_logId, DateTime.UtcNow, Environment.CurrentManagedThreadId, _source, logLevel, message, exception);

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