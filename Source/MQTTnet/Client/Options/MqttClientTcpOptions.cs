// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientTcpOptions : IMqttClientChannelOptions
    {
        EndPoint _remoteEndpoint;
        public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

        public int BufferSize { get; set; } = 8192;

        /// <summary>
        ///     Gets or sets whether the underlying socket should run in dual mode.
        ///     Leaving this _null_ will avoid setting this value at socket level.
        ///     Setting this a value other than _null_ will throw an exception when only IPv4 is supported on the machine.
        /// </summary>
        public bool? DualMode { get; set; }

        [Obsolete("Use RemoteEndpoint or MqttClientOptionsBuilder instead.")]
        public int? Port { get; set; }

        [Obsolete("Use RemoteEndpoint or MqttClientOptionsBuilder instead.")]
        public string Server { get; set; }

        public LingerOption LingerState { get; set; } = new LingerOption(true, 0);

        /// <summary>
        ///     Gets the local endpoint (network card) which is used by the client.
        ///     Set it to _null_ to let the OS select the network card.
        /// </summary>
        public EndPoint LocalEndpoint { get; set; }

        /// <summary>
        ///     Enables or disables the Nagle algorithm for the socket.
        ///     This is only supported for TCP.
        ///     For other protocol types the value is ignored.
        ///     Default: true
        /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        ///     The MQTT transport is usually TCP but when using other endpoint types like
        ///     unix sockets it must be changed (IP for unix sockets).
        /// </summary>
        public ProtocolType ProtocolType { get; set; } = ProtocolType.Tcp;

        public EndPoint RemoteEndpoint
        {
            get => _remoteEndpoint;
            set
            {
                _remoteEndpoint = value;

                if (_remoteEndpoint is DnsEndPoint dnsEndPoint)
                {
                    Server = dnsEndPoint.Host;
                    Port = dnsEndPoint.Port;
                }
                else if (_remoteEndpoint is IPEndPoint ipEndPoint)
                {
                    Server = ipEndPoint.Address.ToString();
                    Port = ipEndPoint.Port;
                }
            }
        }

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public override string ToString()
        {
            if (RemoteEndpoint != null)
            {
                return RemoteEndpoint.ToString();
            }

            if (!string.IsNullOrEmpty(Server))
            {
                return $"{Server}:{GetPort()}";
            }

            return string.Empty;
        }

        int GetPort()
        {
            if (Port.HasValue)
            {
                return Port.Value;
            }

            if (TlsOptions?.UseTls == true)
            {
                return MqttPorts.Secure;
            }

            return MqttPorts.Default;
        }
    }
}