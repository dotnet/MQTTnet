using System;

namespace MQTTnet.Core.Diagnostics
{
    public static class MqttTrace
    {
        public static event EventHandler<MqttTraceMessagePublishedEventArgs> TraceMessagePublished;

        public static void Verbose(string source, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Verbose, null, message, parameters);
        }

        public static void Information(string source, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Information, null, message, parameters);
        }

        public static void Warning(string source, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Warning, null, message, parameters);
        }

        public static void Warning(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Warning, exception, message, parameters);
        }

        public static void Error(string source, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Error, null, message, parameters);
        }

        public static void Error(string source, Exception exception, string message, params object[] parameters)
        {
            Publish(source, MqttTraceLevel.Error, exception, message, parameters);
        }

        private static void Publish(string source, MqttTraceLevel traceLevel, Exception exception, string message, params object[] parameters)
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
                    Error(nameof(MqttTrace), formatException, "Error while tracing message: " + message);
                    return;
                }
            }

            handler.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, traceLevel, message, exception));
        }
    }
}
