using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogMessagePublishedEventArgs : EventArgs
    {
        public MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage)
        {
            TraceMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        [Obsolete("Use new proeprty LogMessage instead.")]
        public MqttNetLogMessage TraceMessage { get; }

        public MqttNetLogMessage LogMessage { get; }
    }
}
