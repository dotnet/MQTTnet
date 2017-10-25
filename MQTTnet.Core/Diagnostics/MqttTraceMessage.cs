using System;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetTraceMessage
    {
        public MqttNetTraceMessage(DateTime timestamp, int threadId, string source, LogLevel level, string message, Exception exception)
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

        public LogLevel Level { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }
}
