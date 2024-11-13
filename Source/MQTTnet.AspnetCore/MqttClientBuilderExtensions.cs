// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Adapter;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MQTTnet.AspNetCore
{
    public static class MqttClientBuilderExtensions
    {
        const string SocketConnectionFactoryTypeName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketConnectionFactory";
        const string SocketConnectionFactoryAssemblyName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets";

        /// <summary>
        /// Replace the implementation of IMqttClientAdapterFactory to AspNetCoreMqttClientAdapterFactory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, SocketConnectionFactoryTypeName, SocketConnectionFactoryAssemblyName)]
        public static IMqttClientBuilder UseAspNetCoreMqttClientAdapterFactory(this IMqttClientBuilder builder)
        {
            if (!builder.Services.Any(s => s.ServiceType == typeof(IConnectionFactory)))
            {
                var socketConnectionFactoryType = Assembly.Load(SocketConnectionFactoryAssemblyName).GetType(SocketConnectionFactoryTypeName);
                builder.Services.AddSingleton(typeof(IConnectionFactory), socketConnectionFactoryType);
            }

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
