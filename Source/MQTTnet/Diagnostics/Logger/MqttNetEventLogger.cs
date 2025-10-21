// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Diagnostics.Logger;

/// <summary>
///     This logger fires an event when a new message was published.
/// </summary>
public sealed class MqttNetEventLogger(string logId = null) : IMqttNetLogger
{
    public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

    public bool IsEnabled => LogMessagePublished != null;

    public string LogId { get; } = logId;

    public IFormatProvider FormatProvider { get; set; }

    public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
    {
        var eventHandler = LogMessagePublished;
        if (eventHandler == null)
        {
            // No listener is attached so we can step out.
            // Keep a reference to the handler because the handler
            // might be null after preparing the message.
            return;
        }

        if (parameters?.Length > 0 && message?.Length > 0)
        {
            try
            {
                message = string.Format(FormatProvider, message, parameters);
            }
            catch (FormatException)
            {
                message = "MESSAGE FORMAT INVALID: " + message;
            }
        }

        // We only use UTC here to improve performance. Using a local date time
        // would require to load the time zone settings!
        var logMessage = new MqttNetLogMessage
        {
            LogId = LogId,
            Timestamp = DateTime.UtcNow,
            Source = source,
            ThreadId = Environment.CurrentManagedThreadId,
            Level = level,
            Message = message,
            Exception = exception
        };

        eventHandler(this, new MqttNetLogMessagePublishedEventArgs(logMessage));
    }
}