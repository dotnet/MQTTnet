using System;

namespace MQTTnet.Diagnostics
{
    public class MqttNetLogMessagePublishedEventArgs : EventArgs
    {
        public MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage)
        {
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));

#pragma warning disable CS0618 // Type or member is obsolete
            TraceMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Obsolete("Use new proeprty LogMessage instead.")]
        public MqttNetLogMessage TraceMessage { get; }

        public MqttNetLogMessage LogMessage { get; }
    }
}
