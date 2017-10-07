using System;

namespace MQTTnet.Core.Diagnostics
{
    public static class MqttNetTrace
    {
        public static event EventHandler<MqttNetTraceMessagePublishedEventArgs> TraceMessagePublished;

        public static void Verbose(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Verbose, null, message, parameters);
        }

        public static void Information(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Information, null, message, parameters);
        }

        public static void Warning(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Warning, null, message, parameters);
        }

        public static void Warning(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Warning, exception, message, parameters);
        }

        public static void Error(string source, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Error, null, message, parameters);
        }

        public static void Error(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttNetTraceLevel.Error, exception, message, parameters);
        }

        private static void Publish(string source, MqttNetTraceLevel traceLevel, Exception exception, string message, params object[] parameters)
        {
            var handler = TraceMessagePublished;
            if (handler == null)
            {
                return;
            }

            if (parameters?.Length > 0)
            {
                try
                {
                    message = string.Format(message, parameters);
                }
                catch (Exception formatException)
                {
                    Error(nameof(MqttNetTrace), formatException, "Error while tracing message: " + message);
                    return;
                }
            }

            handler.Invoke(null, new MqttNetTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, traceLevel, message, exception));
        }
    }
}
