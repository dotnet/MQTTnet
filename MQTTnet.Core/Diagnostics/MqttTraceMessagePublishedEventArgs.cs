using System;

namespace MQTTnet.Core.Diagnostics
{
    public class MqttTraceMessagePublishedEventArgs : EventArgs
    {
        public MqttTraceMessagePublishedEventArgs(int threadId, string source, MqttTraceLevel level, string message, Exception exception)
        {
            ThreadId = threadId;
            Source = source;
            Level = level;
            Message = message;
            Exception = exception;
        }

        public int ThreadId { get; }

        public string Source { get; }

        public MqttTraceLevel Level { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }
}
