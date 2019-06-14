using System;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests.Mockups
{
    public class TestLogger : IMqttNetLogger, IMqttNetChildLogger
    {
        public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        IMqttNetChildLogger IMqttNetLogger.CreateChildLogger(string source)
        {
            return new MqttNetChildLogger(this, source);
        }

        IMqttNetChildLogger IMqttNetChildLogger.CreateChildLogger(string source)
        {
            return new MqttNetChildLogger(this, source);
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
    }
}
