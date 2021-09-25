using System;

namespace MQTTnet.Diagnostics.Logger
{
    public sealed class MqttNetLogMessagePublishedEventArgs : EventArgs
    {
        public MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage)
        {
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        public MqttNetLogMessage LogMessage { get; }
    }
}
