using Microsoft.Extensions.Logging;
using System;

namespace MQTTnet.Core.Tests
{
    public class TestLogger<T> : IDisposable, ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public void Dispose()
        {
        }
    }
}
