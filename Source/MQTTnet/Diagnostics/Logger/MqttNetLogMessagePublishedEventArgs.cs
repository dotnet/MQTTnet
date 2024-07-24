// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Diagnostics.Logger
{
    public sealed class MqttNetLogMessagePublishedEventArgs : EventArgs
    {
        public MqttNetLogMessagePublishedEventArgs(MqttNetLogMessage logMessage)
        {
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        public MqttNetLogMessage LogMessage { get; }
    }
}
