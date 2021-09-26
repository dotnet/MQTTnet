using System;

namespace MQTTnet.Diagnostics.Logger
{
    /// <summary>
    /// This logger does nothing with the messages.
    /// </summary>
    public sealed class MqttNetNullLogger : IMqttNetLogger
    {
        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
        }
    }
}