// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacketProperties
    {
        public uint? WillDelayInterval { get; set; }

        public uint SessionExpiryInterval { get; set; }

        public string AuthenticationMethod { get; set; }

        public byte[] AuthenticationData { get; set; }

        public bool RequestProblemInformation { get; set; } = true;

        public bool RequestResponseInformation { get; set; }

        public ushort? ReceiveMaximum { get; set; }

        public ushort? TopicAliasMaximum { get; set; }

        public uint? MaximumPacketSize { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}