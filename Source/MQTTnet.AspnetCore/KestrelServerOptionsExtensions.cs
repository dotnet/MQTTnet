using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Exceptions;
using MQTTnet.Server;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.AspNetCore
{
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// Listen all endponts in MqttServerOptions
        /// <para>• The properties of the DefaultEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// <para>• The properties of the TlsEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// </summary>
        /// <param name="kestrel"></param>
        /// <exception cref="MqttConfigurationException"></exception>
        /// <returns></returns>
        public static KestrelServerOptions ListenMqtt(this KestrelServerOptions kestrel)
        {
            return kestrel.ListenMqtt(tls => { });
        }

        /// <summary>
        /// Listen all endponts in MqttServerOptions
        /// <para>• The properties of the DefaultEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// <para>• The properties of the TlsEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="serverCertificate"></param>
        /// <exception cref="MqttConfigurationException"></exception>
        /// <returns></returns>
        public static KestrelServerOptions ListenMqtt(this KestrelServerOptions kestrel, X509Certificate2 serverCertificate)
        {
            return kestrel.ListenMqtt(tls => tls.ServerCertificate = serverCertificate);
        }

        /// <summary>
        /// Listen all endponts in MqttServerOptions
        /// <para>• The properties of the DefaultEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// <para>• The properties of the TlsEndpointOptions will be ignored except for the BoundInterNetworkAddress and port</para>
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="tlsConfigure"></param>
        /// <exception cref="MqttConfigurationException"></exception>
        /// <returns></returns>
        public static KestrelServerOptions ListenMqtt(this KestrelServerOptions kestrel, Action<HttpsConnectionAdapterOptions> tlsConfigure)
        {
            var serverOptions = kestrel.ApplicationServices.GetRequiredService<MqttServerOptions>();
            var connectionHandler = kestrel.ApplicationServices.GetRequiredService<MqttConnectionHandler>();

            if (serverOptions.DefaultEndpointOptions.IsEnabled)
            {
                var endpoint = serverOptions.DefaultEndpointOptions;
                kestrel.Listen(endpoint.BoundInterNetworkV6Address, endpoint.Port, o => o.UseMqtt());
                if (!IPAddress.IPv6Any.Equals(endpoint.BoundInterNetworkV6Address))
                {
                    kestrel.Listen(endpoint.BoundInterNetworkAddress, endpoint.Port, o => o.UseMqtt());
                }
                connectionHandler.ListenFlag = true;
            }

            if (serverOptions.TlsEndpointOptions.IsEnabled)
            {
                var endpoint = serverOptions.TlsEndpointOptions;
                kestrel.Listen(endpoint.BoundInterNetworkV6Address, endpoint.Port, o => o.UseHttps(tlsConfigure).UseMqtt());
                if (!IPAddress.IPv6Any.Equals(endpoint.BoundInterNetworkV6Address))
                {
                    kestrel.Listen(endpoint.BoundInterNetworkAddress, endpoint.Port, o => o.UseHttps(tlsConfigure).UseMqtt());
                }
                connectionHandler.ListenFlag = true;
            }

            if (!connectionHandler.ListenFlag)
            {
                throw new MqttConfigurationException("None of the MqttServerOptions Endpoints are enabled.");
            }
            return kestrel;
        }
    }
}
