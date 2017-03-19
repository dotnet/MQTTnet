using System;

namespace MQTTnet.Core.Diagnostics
{
    public static class MqttTrace
    {
        public static event EventHandler<MqttTraceMessagePublishedEventArgs> TraceMessagePublished;

        public static void Verbose(string source, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Verbose, message, null));
        }

        public static void Information(string source, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Information, message, null));
        }

        public static void Warning(string source, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Warning, message, null));
        }

        public static void Warning(string source, Exception exception, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Warning, message, exception));
        }

        public static void Error(string source, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Error, message, null));
        }

        public static void Error(string source, Exception exception, string message)
        {
            TraceMessagePublished?.Invoke(null, new MqttTraceMessagePublishedEventArgs(Environment.CurrentManagedThreadId, source, MqttTraceLevel.Error, message, exception));
        }
    }
}
