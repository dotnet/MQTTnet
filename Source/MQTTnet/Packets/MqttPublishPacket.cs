// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttPublishPacket : MqttPacketWithIdentifier
    {
        private byte[] _payloadCache = null;
        private ArraySegment<byte> _payloadSegment = EmptyBuffer.ArraySegment;

        public string ContentType { get; set; }

        public byte[] CorrelationData { get; set; }

        public bool Dup { get; set; }

        public uint MessageExpiryInterval { get; set; }

        public byte[] Payload
        {
            get
            {
                // just reference from _payloadSegment.Array
                if (_payloadSegment.Count == _payloadSegment.Array.Length)
                {
                    return _payloadSegment.Array;
                }

                // copy from _payloadSegment
                if (_payloadCache == null)
                {
                    _payloadCache = new byte[_payloadSegment.Count];
                    Array.Copy(_payloadSegment.Array, _payloadSegment.Offset, _payloadCache, 0, _payloadCache.Length);
                }
                return _payloadCache;
            }
            set
            {
                _payloadCache = null;
                _payloadSegment = value == null || value.Length == 0
                    ? EmptyBuffer.ArraySegment
                    : new ArraySegment<byte>(value);
            }
        }

        public ArraySegment<byte> PayloadSegment
        {
            get
            {
                return _payloadSegment;
            }
            set
            {
                _payloadCache = null;
                _payloadSegment = value;
            }
        }

        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; } = MqttPayloadFormatIndicator.Unspecified;

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        public string ResponseTopic { get; set; }

        public bool Retain { get; set; }

        public List<uint> SubscriptionIdentifiers { get; set; }

        public string Topic { get; set; }

        public ushort TopicAlias { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public override string ToString()
        {
            return
                $"Publish: [Topic={Topic}] [PayloadLength={this.PayloadSegment.Count}] [QoSLevel={QualityOfServiceLevel}] [Dup={Dup}] [Retain={Retain}] [PacketIdentifier={PacketIdentifier}]";
        }
    }
}