// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Diagnostics.Logger;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MQTTnet.AspNetCore
{
    public static class MqttBuilderExtensions
    {
        /// <summary>
        /// Use AspNetCoreMqttNetLogger as IMqttNetLogger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IMqttBuilder UseAspNetCoreMqttNetLogger(this IMqttBuilder builder, Action<AspNetCoreMqttNetLoggerOptions> configure)
        {
            builder.Services.Configure(configure);
            return builder.UseAspNetCoreMqttNetLogger();
        }

        /// <summary>
        /// Use AspNetCoreMqttNetLogger as IMqttNetLogger
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttBuilder UseAspNetCoreMqttNetLogger(this IMqttBuilder builder)
        {
            return builder.UseLogger<AspNetCoreMqttNetLogger>();
        }

        /// <summary>
        /// Use MqttNetNullLogger as IMqttNetLogger
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttBuilder UseMqttNetNullLogger(this IMqttBuilder builder)
        {
            return builder.UseLogger<MqttNetNullLogger>();
        }

        /// <summary>
        /// Use a logger
        /// </summary>
        /// <typeparam name="TLogger"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttBuilder UseLogger<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLogger>(this IMqttBuilder builder)
            where TLogger : class, IMqttNetLogger
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IMqttNetLogger, TLogger>());
            return builder;
        }

        private class MqttBuilder(IServiceCollection services) : IMqttBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
