using System;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Core.Diagnostics
{
    public class MqttNetLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly MqttNetTrace _mqttNetTrace;

        public MqttNetLogger(string categoryName, MqttNetTrace mqttNetTrace)
        {
            _categoryName = categoryName;
            _mqttNetTrace = mqttNetTrace;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!MqttNetTrace.HasListeners)
            {
                return;
            }

            var message = formatter(state, exception);
            var traceMessage = new MqttNetTraceMessage(DateTime.Now, Environment.CurrentManagedThreadId, _categoryName, logLevel, message, exception);
            _mqttNetTrace.Publish(traceMessage);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return MqttNetTrace.HasListeners;
        }

        //not supported: async local requires netstandard1.3
        //for implementation see https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.Console/ConsoleLogScope.cs
        public IDisposable BeginScope<TState>(TState state)
        {
            return new DisposableScope();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}