using System;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetTraceMessagePublishedEventArgs : EventArgs
    {
        public MqttNetTraceMessagePublishedEventArgs(int threadId, string source, MqttNetTraceLevel level, string message, Exception exception)
        {
            ThreadId = threadId;
            Source = source;
            Level = level;
            Message = message;
            Exception = exception;
        }

        public int ThreadId { get; }

        public string Source { get; }

        public MqttNetTraceLevel Level { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }
}
