using System;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetTraceMessage
    {
        public MqttNetTraceMessage(DateTime timestamp, int threadId, string source, MqttNetTraceLevel level, string message, Exception exception)
        {
            Timestamp = timestamp;
            ThreadId = threadId;
            Source = source;
            Level = level;
            Message = message;
            Exception = exception;
        }

        public DateTime Timestamp { get; }

        public int ThreadId { get; }

        public string Source { get; }

        public MqttNetTraceLevel Level { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }
}
