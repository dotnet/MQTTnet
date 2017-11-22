using System;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetLogMessagePublishedEventArgs : EventArgs
    {
        public MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage)
        {
            TraceMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        public MqttNetLogMessage TraceMessage { get; }
    }
}
