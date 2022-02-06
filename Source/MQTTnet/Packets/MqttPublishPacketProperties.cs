// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPublishPacketProperties
    {
        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; } = MqttPayloadFormatIndicator.Unspecified;

        public uint MessageExpiryInterval { get; set; }

        public ushort? TopicAlias { get; set; }

        public string ResponseTopic { get; set; }

        public byte[] CorrelationData { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();

        public List<uint> SubscriptionIdentifiers { get; } = new List<uint>();

        public string ContentType { get; set; }
    }
}
