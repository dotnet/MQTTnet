// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttClientWebSocketProxyOptions
    {
        public string Address { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Domain { get; set; }

        public bool BypassOnLocal { get; set; }

        public string[] BypassList { get; set; }
    }
}