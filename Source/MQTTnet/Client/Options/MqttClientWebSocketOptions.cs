// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;

namespace MQTTnet.Client
{
    public sealed class MqttClientWebSocketOptions : IMqttClientChannelOptions
    {
        public CookieContainer CookieContainer { get; set; }

        public ICredentials Credentials { get; set; }

        public MqttClientWebSocketProxyOptions ProxyOptions { get; set; }

        public IDictionary<string, string> RequestHeaders { get; set; }

        public ICollection<string> SubProtocols { get; set; } = new List<string> { "mqtt" };

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public string Uri { get; set; }

        public override string ToString()
        {
            return Uri;
        }

#if !NETSTANDARD1_3
        /// <summary>
        ///     Gets or sets the keep alive interval for the Web Socket connection.
        ///     This is not related to the keep alive interval for the MQTT protocol.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = WebSocket.DefaultKeepAliveInterval;

        /// <summary>
        ///     Gets or sets whether the default (system) credentials should be used when connecting via Web Socket connection.
        ///     This is not related to the credentials which are used for the MQTT protocol.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }
#else
        /// <summary>
        ///     Gets or sets the keep alive interval for the Web Socket connection.
        ///     This is not related to the keep alive interval for the MQTT protocol.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);
#endif
    }
}