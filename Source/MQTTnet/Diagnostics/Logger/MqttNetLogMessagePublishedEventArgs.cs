// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Diagnostics.Logger;

public sealed class MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage) : EventArgs
{
    public MqttNetLogMessage LogMessage { get; } = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
}