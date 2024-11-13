using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using System;

namespace MQTTnet.AspNetCore
{
    public static class MqttServerBuilderExtensions
    {
        /// <summary>
        /// Disable logging
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttServerBuilder UseNullLogger(this IMqttServerBuilder builder)
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IMqttNetLogger>(MqttNetNullLogger.Instance));
            return builder;
        }

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
        /// Add MqttTcpServerAdapter to MqttServer   
        /// </summary>
        /// <remarks>We recommend using ListenOptions.UseMqtt() instead of using MqttTcpServerAdapter in an AspNetCore environment</remarks>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMqttServerBuilder AddMqttTcpServerAdapter(this IMqttServerBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, MqttTcpServerAdapter>());
            return builder;
        }
    }
}
