using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogMessage
    {
        public MqttNetLogMessage(string logId, DateTime timestamp, int threadId, string source, MqttNetLogLevel level, string message, Exception exception)
        {
            LogId = logId;
            Timestamp = timestamp;
            ThreadId = threadId;
            Source = source;
            Level = level;
            Message = message;
            Exception = exception;
        }

        public string LogId { get; }

        public DateTime Timestamp { get; }

        public int ThreadId { get; }

        public string Source { get; }

        public MqttNetLogLevel Level { get; }

        public string Message { get; }

        public Exception Exception { get; }

        public override string ToString()
        {
            var result = $"[{Timestamp:O}] [{LogId}] [{ThreadId}] [{Source}] [{Level}]: {Message}";
            if (Exception != null)
            {
                result += Environment.NewLine + Exception;
            }

            return result;
        }
    }
}
