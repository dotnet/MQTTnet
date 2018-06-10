using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetChildLogger
    {
        IMqttNetChildLogger CreateChildLogger(string source = null);

        void Verbose(string message, params object[] parameters);

        void Info(string message, params object[] parameters);

        void Warning(Exception exception, string message, params object[] parameters);

        void Error(Exception exception, string message, params object[] parameters);
    }
}
