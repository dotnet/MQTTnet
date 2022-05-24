// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace MQTTnet.Client
{
    public sealed class MqttClientTcpOptions : IMqttClientChannelOptions
    {
        public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

        public int BufferSize { get; set; } = 8192;

        public bool? DualMode { get; set; }

        public LingerOption LingerState { get; set; } = new LingerOption(true, 0);

        public bool NoDelay { get; set; } = true;

        public int? Port { get; set; }
        
        public string Server { get; set; }

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public override string ToString()
        {
            return Server + ":" + this.GetPort();
        }
    }
}