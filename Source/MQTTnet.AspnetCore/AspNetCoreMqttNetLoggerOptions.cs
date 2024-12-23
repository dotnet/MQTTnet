// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;
using System;

namespace MQTTnet.AspNetCore
{
    public sealed class AspNetCoreMqttNetLoggerOptions
    {
        public string? CategoryNamePrefix { get; set; } = "MQTTnet.AspNetCore.";

        public Func<MqttNetLogLevel, LogLevel> LogLevelConverter { get; set; } = ConvertLogLevel;

        private static LogLevel ConvertLogLevel(MqttNetLogLevel level)
        { 
            return level switch
            {
                MqttNetLogLevel.Verbose => LogLevel.Trace,
                MqttNetLogLevel.Info => LogLevel.Information,
                MqttNetLogLevel.Warning => LogLevel.Warning,
                MqttNetLogLevel.Error => LogLevel.Error,
                _ => LogLevel.None
            };
        }
    }
}
