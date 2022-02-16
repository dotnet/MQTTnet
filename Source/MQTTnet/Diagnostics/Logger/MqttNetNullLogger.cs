// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Diagnostics
{
    /// <summary>
    /// This logger does nothing with the messages.
    /// </summary>
    public sealed class MqttNetNullLogger : IMqttNetLogger
    {
        public static MqttNetNullLogger Instance { get; } = new MqttNetNullLogger();
        
        public bool IsEnabled { get; }

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
        }
    }
}