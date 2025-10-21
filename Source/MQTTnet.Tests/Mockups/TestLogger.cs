// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.Mockups;

public sealed class TestLogger : IMqttNetLogger
{
    public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

    public bool IsEnabled { get; } = true;

    public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
    {
        LogMessagePublished?.Invoke(this, new MqttNetLogMessagePublishedEventArgs(new MqttNetLogMessage
        {
            Level = level,
            Message = string.Format(CultureInfo.InvariantCulture, message, parameters),
            Exception = exception
        }));
    }
}