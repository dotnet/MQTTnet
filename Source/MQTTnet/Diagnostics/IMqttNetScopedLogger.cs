using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetScopedLogger
    {
        IMqttNetScopedLogger CreateScopedLogger(string source);

        void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception);
    }
}
