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

        public IMqttNetChildLogger CreateChildLogger(string source = null)
        {
            return new MqttNetChildLogger(this, source);
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
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

            var traceMessage = new MqttNetLogMessage(_logId, DateTime.UtcNow, Environment.CurrentManagedThreadId, source, logLevel, message, exception);

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