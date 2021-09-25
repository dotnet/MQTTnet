using System;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestLogger : IMqttNetLogger
    {
        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;
        
        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(new MqttNetLogMessage
            {
                Level = logLevel,
                Message = string.Format(message, parameters),
                Exception = exception
            }));
        }
    }
}
