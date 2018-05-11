using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetLogger
    {
        event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        void Verbose<TSource>(string message, params object[] parameters);

        void Verbose(object source, string message, params object[] parameters);

        void Info<TSource>(string message, params object[] parameters);

        void Info(object source, string message, params object[] parameters);

        void Warning<TSource>(Exception exception, string message, params object[] parameters);

        void Warning(object source, Exception exception, string message, params object[] parameters);

        void Error<TSource>(Exception exception, string message, params object[] parameters);

        void Error(object source, Exception exception, string message, params object[] parameters);
    }
}
