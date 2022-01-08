using System;

namespace MQTTnet.Diagnostics.Logger
{
    public interface IMqttNetLogger
    {
        bool IsEnabled { get; }
        
        void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception);
    }
}
