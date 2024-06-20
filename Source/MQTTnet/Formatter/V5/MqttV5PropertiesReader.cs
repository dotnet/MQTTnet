// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Buffers;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V5
{
    public struct MqttV5PropertiesReader
    {
        readonly MqttBufferReader _body;
        readonly int _length;
        readonly int _targetOffset;

        public MqttV5PropertiesReader(MqttBufferReader body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));

            if (!body.EndOfStream)
            {
                _length = (int)body.ReadVariableByteInteger();
            }
            else
            {
                _length = 0;
            }

            _targetOffset = body.Position + _length;

            CollectedUserProperties = null;
            CurrentPropertyId = MqttPropertyId.None;
        }

        public List<MqttUserProperty> CollectedUserProperties { get; private set; }

        public MqttPropertyId CurrentPropertyId { get; private set; }

        public bool MoveNext()
        {
            while (true)
            {
                if (_length == 0)
                {
                    return false;
                }

                if (_body.Position >= _targetOffset)
                {
                    return false;
                }

                CurrentPropertyId = (MqttPropertyId)_body.ReadByte();

                // User properties are special because they can appear multiple times in the
                // buffer and at any position. So we collect them here to expose them as a 
                // final result list.
                if (CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    var name = _body.ReadString();
                    var value = _body.ReadString();

                    if (CollectedUserProperties == null)
                    {
                        CollectedUserProperties = new List<MqttUserProperty>();
                    }

                    CollectedUserProperties.Add(new MqttUserProperty(name, value));
                    continue;
                }

                return true;
            }
        }

        public string ReadAssignedClientIdentifier()
        {
            return _body.ReadString();
        }

        public byte[] ReadAuthenticationData()
        {
            return _body.ReadBinaryData();
        }

        public string ReadAuthenticationMethod()
        {
            return _body.ReadString();
        }

        public string ReadContentType()
        {
            return _body.ReadString();
        }

        public byte[] ReadCorrelationData()
        {
            return _body.ReadBinaryData();
        }

        public uint ReadMaximumPacketSize()
        {
            return _body.ReadFourByteInteger();
        }

        public MqttQualityOfServiceLevel ReadMaximumQoS()
        {
            var value = _body.ReadByte();
            if (value > 1)
            {
                throw new MqttProtocolViolationException($"Unexpected Maximum QoS value: {value}");
            }

            return (MqttQualityOfServiceLevel)value;
        }

        public uint ReadMessageExpiryInterval()
        {
            return _body.ReadFourByteInteger();
        }

        public MqttPayloadFormatIndicator ReadPayloadFormatIndicator()
        {
            return (MqttPayloadFormatIndicator)_body.ReadByte();
        }

        public string ReadReasonString()
        {
            return _body.ReadString();
        }

        public ushort ReadReceiveMaximum()
        {
            return _body.ReadTwoByteInteger();
        }

        public string ReadResponseInformation()
        {
            return _body.ReadString();
        }

        public string ReadResponseTopic()
        {
            return _body.ReadString();
        }

        public bool ReadRetainAvailable()
        {
            return _body.ReadByte() == 1;
        }

        public ushort ReadServerKeepAlive()
        {
            return _body.ReadTwoByteInteger();
        }

        public string ReadServerReference()
        {
            return _body.ReadString();
        }

        public uint ReadSessionExpiryInterval()
        {
            return _body.ReadFourByteInteger();
        }

        public bool ReadSharedSubscriptionAvailable()
        {
            return _body.ReadByte() == 1;
        }

        public uint ReadSubscriptionIdentifier()
        {
            return _body.ReadVariableByteInteger();
        }

        public bool ReadSubscriptionIdentifiersAvailable()
        {
            return _body.ReadByte() == 1;
        }

        public ushort ReadTopicAlias()
        {
            return _body.ReadTwoByteInteger();
        }

        public ushort ReadTopicAliasMaximum()
        {
            return _body.ReadTwoByteInteger();
        }

        public bool ReadWildcardSubscriptionAvailable()
        {
            return _body.ReadByte() == 1;
        }

        public uint ReadWillDelayInterval()
        {
            return _body.ReadFourByteInteger();
        }

        public bool RequestProblemInformation()
        {
            return _body.ReadByte() == 1;
        }

        public bool RequestResponseInformation()
        {
            return _body.ReadByte() == 1;
        }

        public void ThrowInvalidPropertyIdException(Type type)
        {
            throw new MqttProtocolViolationException($"Property ID '{CurrentPropertyId}' is not supported for package type '{type.Name}'.");
        }
    }
}