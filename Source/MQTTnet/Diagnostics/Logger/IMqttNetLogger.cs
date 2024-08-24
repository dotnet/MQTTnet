// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Diagnostics.Logger
{
    public interface IMqttNetLogger
    {
        bool IsEnabled { get; }
        
        void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception);
    }
}
