using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetLogger
    {
        event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        IMqttNetLogger CreateChildLogger(string source = null);

        void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception);
    }
}
