using System;

namespace MQTTnet.Diagnostics
{
    public interface IMqttNetLogger
    {
        event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        void Trace<TSource>(string message, params object[] parameters);

        void Info<TSource>(string message, params object[] parameters);

        void Warning<TSource>(Exception exception, string message, params object[] parameters);

        void Warning<TSource>(string message, params object[] parameters);

        void Error<TSource>(Exception exception, string message, params object[] parameters);

        void Error<TSource>(string message, params object[] parameters);
    }
}
