using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetLogger
    {
        IMqttNetScopedLogger CreateScopedLogger(string source);

        void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception);
    }
}
