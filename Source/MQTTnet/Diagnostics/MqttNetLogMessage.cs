using System;

namespace MQTTnet.Diagnostics
{
    public sealed class MqttNetLogMessage
    {
        public string LogId { get; set; }

        public DateTime Timestamp { get; set; }

        public int ThreadId { get; set; }

        public string Source { get; set; }

        public MqttNetLogLevel Level { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

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
