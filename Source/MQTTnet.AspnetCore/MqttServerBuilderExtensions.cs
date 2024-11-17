// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Server;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MQTTnet.AspNetCore
{
    public static class MqttServerBuilderExtensions
    {
        /// <summary>
        /// Configure MqttServerOptionsBuilder
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IMqttServerBuilder ConfigureMqttServer(this IMqttServerBuilder builder, Action<MqttServerOptionsBuilder> configure)
        {
            builder.Services.Configure(configure);
            return builder;
        }

        /// <summary>
        /// Configure MqttServerStopOptionsBuilder
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IMqttServerBuilder ConfigureMqttServerStop(this IMqttServerBuilder builder, Action<MqttServerStopOptionsBuilder> configure)
        {
            builder.Services.Configure(configure);
            return builder;
        }
         
        /// <summary>
        /// Add an IMqttServerAdapter to MqttServer
        /// </summary>
        /// <typeparam name="TMqttServerAdapter"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttServerBuilder AddMqttServerAdapter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMqttServerAdapter>(this IMqttServerBuilder builder)
            where TMqttServerAdapter : class, IMqttServerAdapter
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, TMqttServerAdapter>());
            return builder;
        }
    }
}
