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
        /// Disable logging
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttBuilder UseNullLogger(this IMqttBuilder builder)
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
