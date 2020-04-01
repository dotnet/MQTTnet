using MQTTnet.Diagnostics;
using System;

namespace MQTTnet.Tests.Mockups
{
    public class TestLogger : IMqttNetLogger
    {
        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public IMqttNetLogger CreateChildLogger(string source = null)
        {
            return new TestLogger();
        }

        public void Verbose(string message, params object[] parameters)
        {
        }

        public void Info(string message, params object[] parameters)
        {
        }

        public void Warning(Exception exception, string message, params object[] parameters)
        {
        }

        public void Error(Exception exception, string message, params object[] parameters)
        {
        }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
        }

        public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}
