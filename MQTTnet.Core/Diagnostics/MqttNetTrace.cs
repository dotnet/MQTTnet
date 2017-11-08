using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Core.Diagnostics
{
    public class MqttNetTrace : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, MqttNetLogger> _loggers = new ConcurrentDictionary<string, MqttNetLogger>();

        public static event EventHandler<MqttNetTraceMessagePublishedEventArgs> TraceMessagePublished;

        public static bool HasListeners => TraceMessagePublished != null;

        public void Publish(MqttNetTraceMessage traceMessage)
        {
            TraceMessagePublished?.Invoke(this, new MqttNetTraceMessagePublishedEventArgs(traceMessage));
        }

        public void Dispose()
        {
            TraceMessagePublished = null;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private MqttNetLogger CreateLoggerImplementation(string categoryName)
        {
            return new MqttNetLogger(categoryName, this);
        }
    }
}
