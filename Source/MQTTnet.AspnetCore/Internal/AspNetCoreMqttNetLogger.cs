// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Diagnostics.Logger;
using System;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttNetLogger : IMqttNetLogger
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AspNetCoreMqttNetLoggerOptions _loggerOptions;

        public bool IsEnabled => true;

        public AspNetCoreMqttNetLogger(
            ILoggerFactory loggerFactory,
            IOptions<AspNetCoreMqttNetLoggerOptions> loggerOptions)
        {
            _loggerFactory = loggerFactory;
            _loggerOptions = loggerOptions.Value;
        }

        public void Publish(MqttNetLogLevel logLevel, string? source, string? message, object[]? parameters, Exception? exception)
        {
            var categoryName = $"{_loggerOptions.CategoryNamePrefix}{source}";
            var logger = _loggerFactory.CreateLogger(categoryName);
            var level = _loggerOptions.LogLevelConverter(logLevel);
            logger.Log(level, exception, message, parameters ?? []);
        }
    }
}
