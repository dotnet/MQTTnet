// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageBuilder
    {
        string _contentType;
        byte[] _correlationData;
        uint _messageExpiryInterval;

        MqttPayloadFormatIndicator _payloadFormatIndicator;
        ArraySegment<byte> _payloadSegment;
        MqttQualityOfServiceLevel _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
        string _responseTopic;
        bool _retain;
        List<uint> _subscriptionIdentifiers;
        string _topic;
        ushort _topicAlias;
        List<MqttUserProperty> _userProperties;

        public MqttApplicationMessage Build()
        {
            if (_topicAlias == 0 && string.IsNullOrEmpty(_topic))
            {
                throw new MqttProtocolViolationException("Topic or TopicAlias is not set.");
            }

            var applicationMessage = new MqttApplicationMessage
            {
                Topic = _topic,
                PayloadSegment = _payloadSegment,
                QualityOfServiceLevel = _qualityOfServiceLevel,
                Retain = _retain,
                ContentType = _contentType,
                ResponseTopic = _responseTopic,
                CorrelationData = _correlationData,
                TopicAlias = _topicAlias,
                SubscriptionIdentifiers = _subscriptionIdentifiers,
                MessageExpiryInterval = _messageExpiryInterval,
                PayloadFormatIndicator = _payloadFormatIndicator,
                UserProperties = _userProperties
            };

            return applicationMessage;
        }

        /// <summary>
        ///     Adds the content type to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        /// <summary>
        ///     Adds the correlation data to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithCorrelationData(byte[] correlationData)
        {
            _correlationData = correlationData;
            return this;
        }

        /// <summary>
        ///     Adds the message expiry interval in seconds to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithMessageExpiryInterval(uint messageExpiryInterval)
        {
            _messageExpiryInterval = messageExpiryInterval;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(byte[] payload)
        {
            _payloadSegment = payload == null || payload.Length == 0 ? EmptyBuffer.ArraySegment : new ArraySegment<byte>(payload);
            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(ArraySegment<byte> payloadSegment)
        {
            _payloadSegment = payloadSegment;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(IEnumerable<byte> payload)
        {
            if (payload == null)
            {
                return WithPayload(default(byte[]));
            }

            if (payload is byte[] byteArray)
            {
                return WithPayload(byteArray);
            }

            if (payload is ArraySegment<byte> arraySegment)
            {
                return WithPayloadSegment(arraySegment);
            }

            return WithPayload(payload.ToArray());
        }

        public MqttApplicationMessageBuilder WithPayload(Stream payload)
        {
            return payload == null ? WithPayload(default(byte[])) : WithPayload(payload, payload.Length - payload.Position);
        }

        public MqttApplicationMessageBuilder WithPayload(Stream payload, long length)
        {
            if (payload == null || length == 0)
            {
                return WithPayload(default(byte[]));
            }

            var payloadBuffer = new byte[length];
            var totalRead = 0;
            do
            {
                var bytesRead = payload.Read(payloadBuffer, totalRead, payloadBuffer.Length - totalRead);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
            } while (totalRead < length);

            return WithPayload(payloadBuffer);
        }

        public MqttApplicationMessageBuilder WithPayload(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return WithPayload(default(byte[]));
            }

            var payloadBuffer = Encoding.UTF8.GetBytes(payload);
            return WithPayload(payloadBuffer);
        }

        /// <summary>
        ///     Adds the payload format indicator to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithPayloadFormatIndicator(MqttPayloadFormatIndicator payloadFormatIndicator)
        {
            _payloadFormatIndicator = payloadFormatIndicator;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayloadSegment(ArraySegment<byte> payloadSegment)
        {
            _payloadSegment = payloadSegment;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayloadSegment(ReadOnlyMemory<byte> payloadSegment)
        {
            return MemoryMarshal.TryGetArray(payloadSegment, out var segment) ? WithPayloadSegment(segment) : WithPayload(payloadSegment.ToArray());
        }

        /// <summary>
        ///     The quality of service level.
        ///     The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message
        ///     that defines the guarantee of delivery for a specific message.
        ///     There are 3 QoS levels in MQTT:
        ///     - At most once  (0): Message gets delivered no time, once or multiple times.
        ///     - At least once (1): Message gets delivered at least once (one time or more often).
        ///     - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        public MqttApplicationMessageBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            return this;
        }

        /// <summary>
        ///     Adds the response topic to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithResponseTopic(string responseTopic)
        {
            _responseTopic = responseTopic;
            return this;
        }

        /// <summary>
        ///     A value indicating whether the message should be retained or not.
        ///     A retained message is a normal MQTT message with the retained flag set to true.
        ///     The broker stores the last retained message and the corresponding QoS for that topic.
        /// </summary>
        public MqttApplicationMessageBuilder WithRetainFlag(bool value = true)
        {
            _retain = value;
            return this;
        }

        /// <summary>
        ///     Adds the subscription identifier to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithSubscriptionIdentifier(uint subscriptionIdentifier)
        {
            if (_subscriptionIdentifiers == null)
            {
                _subscriptionIdentifiers = new List<uint>();
            }

            _subscriptionIdentifiers.Add(subscriptionIdentifier);
            return this;
        }

        /// <summary>
        ///     The MQTT topic.
        ///     In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected
        ///     client.
        ///     The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level
        ///     separator).
        /// </summary>
        public MqttApplicationMessageBuilder WithTopic(string topic)
        {
            _topic = topic;
            return this;
        }

        /// <summary>
        ///     Adds the topic alias to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithTopicAlias(ushort topicAlias)
        {
            _topicAlias = topicAlias;
            return this;
        }

        /// <summary>
        ///     Adds the user property to the message.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttApplicationMessageBuilder WithUserProperty(string name, string value)
        {
            if (_userProperties == null)
            {
                _userProperties = new List<MqttUserProperty>();
            }

            _userProperties.Add(new MqttUserProperty(name, value));
            return this;
        }
    }
}