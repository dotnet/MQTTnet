using System;

namespace MQTTnet.Core.Diagnostics
{
    public static class MqttTrace
    {
        public static event EventHandler<MqttTraceMessagePublishedEventArgs> TraceMessagePublished;

        public static void Verbose(string source, string message)
        {
            Publish(source, MqttTraceLevel.Verbose, null, message);
        }

        public static void Information(string source, string message)
        {
            Publish(source, MqttTraceLevel.Information, null, message);
        }

        public static void Warning(string source, string message)
        {
            Publish(source, MqttTraceLevel.Warning, null, message);
        }

        public static void Warning(string source, Exception exception, string message)
        {
            Publish(source, MqttTraceLevel.Warning, exception, message);
        }

        public static void Error(string source, string message)
        {
            Publish(source, MqttTraceLevel.Error, null, message);
        }

        public static void Error(string source, Exception exception, string message)
        {
            Publish(source, MqttTraceLevel.Error, exception, message);
        }

        private static void Publish(string source, MqttTraceLevel traceLevel, Exception exception, string message)
        {
            var handler = TraceMessagePublished;
            if (handler == null)
            {
                return;
            }

            message = string.Format(message, 1);
            handler.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, traceLevel, message, exception));
        }
    }
}
