// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Adapter;
using MQTTnet.Implementations;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MQTTnet.AspNetCore
{
    public static class MqttClientBuilderExtensions
    {
        /// <summary>
        /// Replace the implementation of IMqttClientAdapterFactory to MQTTnet.Implementations.MqttClientAdapterFactory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttClientBuilder UseMQTTnetMqttClientAdapterFactory(this IMqttClientBuilder builder)
        {
            return builder.UseMqttClientAdapterFactory<MqttClientAdapterFactory>();
        }

        /// <summary>
        /// Replace the implementation of IMqttClientAdapterFactory to AspNetCoreMqttClientAdapterFactory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttClientBuilder UseAspNetCoreMqttClientAdapterFactory(this IMqttClientBuilder builder)
        {
            return builder.UseMqttClientAdapterFactory<AspNetCoreMqttClientAdapterFactory>();
        }

        /// <summary>
        /// Replace the implementation of IMqttClientAdapterFactory to TMqttClientAdapterFactory
        /// </summary>
        /// <typeparam name="TMqttClientAdapterFactory"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttClientBuilder UseMqttClientAdapterFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMqttClientAdapterFactory>(this IMqttClientBuilder builder)
            where TMqttClientAdapterFactory : class, IMqttClientAdapterFactory
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IMqttClientAdapterFactory, TMqttClientAdapterFactory>());
            return builder;
        }
    }
}
