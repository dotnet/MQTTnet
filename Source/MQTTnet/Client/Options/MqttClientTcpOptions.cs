// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace MQTTnet.Client
{
    public class MqttClientTcpOptions : IMqttClientChannelOptions
    {
        public string Server { get; set; }

        public int? Port { get; set; }

        public int BufferSize { get; set; } = 8192;

        public bool? DualMode { get; set; }

        public bool NoDelay { get; set; } = true;

        public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public override string ToString()
        {
            return Server + ":" + this.GetPort();
        }
    }
}
