using MQTTnet.Diagnostics;
using System;

namespace MQTTnet.Tests.Mockups
{
    public class TestLogger : IMqttNetLogger
    {
        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public IMqttNetScopedLogger CreateScopedLogger(string source)
        {
            return new MqttNetScopedLogger(this, source);
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(new MqttNetLogMessage
            {
                Level = logLevel,
                Message = message
            }));
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}
