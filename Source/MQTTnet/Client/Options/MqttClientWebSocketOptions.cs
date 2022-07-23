// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public sealed class MqttClientWebSocketOptions : IMqttClientChannelOptions
    {
        public CookieContainer CookieContainer { get; set; }
        
        public MqttClientWebSocketProxyOptions ProxyOptions { get; set; }

        public IDictionary<string, string> RequestHeaders { get; set; }

        public ICollection<string> SubProtocols { get; set; } = new List<string> { "mqtt" };

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();
        
        public string Uri { get; set; }

        public override string ToString()
        {
            return Uri;
        }
    }
}