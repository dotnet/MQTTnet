// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Formatter;

namespace MQTTnet.Adapter
{
    public readonly struct ReceivedMqttPacket
    {
        public static readonly ReceivedMqttPacket Empty = new ReceivedMqttPacket();
        
        public ReceivedMqttPacket(byte fixedHeader, IMqttPacketBodyReader bodyReader, int totalLength)
        {
            FixedHeader = fixedHeader;
            BodyReader = bodyReader ?? throw new ArgumentNullException(nameof(bodyReader));
            TotalLength = totalLength;
        }

        public byte FixedHeader { get; }

        public IMqttPacketBodyReader BodyReader { get; }

        public int TotalLength { get; }
    }
}
