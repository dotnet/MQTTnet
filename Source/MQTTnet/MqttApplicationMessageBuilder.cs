// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageBuilder
    {
        string _contentType;
        byte[] _correlationData;
        uint _messageExpiryInterval;
        byte[] _payload;
        MqttPayloadFormatIndicator _payloadFormatIndicator;
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
                Payload = _payload,
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
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
        public MqttApplicationMessageBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        /// <summary>
        ///     Adds the correlation data to the message.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="correlationData">
        ///     The correlation data.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
        public MqttApplicationMessageBuilder WithCorrelationData(byte[] correlationData)
        {
            _correlationData = correlationData;
            return this;
        }

        /// <summary>
        ///     Adds the message expiry interval in seconds to the message.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="messageExpiryInterval">
        ///     The message expiry interval.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
        public MqttApplicationMessageBuilder WithMessageExpiryInterval(uint messageExpiryInterval)
        {
            _messageExpiryInterval = messageExpiryInterval;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(byte[] payload)
        {
            _payload = payload;
            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(IEnumerable<byte> payload)
        {
            if (payload == null)
            {
                _payload = null;
                return this;
            }

            _payload = payload as byte[] ?? payload.ToArray();

            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(Stream payload)
        {
            if (payload == null)
            {
                _payload = null;
                return this;
            }

            return WithPayload(payload, payload.Length - payload.Position);
        }

        public MqttApplicationMessageBuilder WithPayload(Stream payload, long length)
        {
            if (payload == null)
            {
                _payload = null;
                return this;
            }

            if (payload.Length == 0)
            {
                _payload = null;
            }
            else
            {
                _payload = new byte[length];

                var totalRead = 0;
                do
                {
                    var bytesRead = payload.Read(_payload, totalRead, _payload.Length - totalRead);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalRead += bytesRead;
                } while (totalRead < length);
            }

            return this;
        }

        public MqttApplicationMessageBuilder WithPayload(string payload)
        {
            if (payload == null)
            {
                _payload = null;
                return this;
            }

            _payload = string.IsNullOrEmpty(payload) ? null : Encoding.UTF8.GetBytes(payload);
            return this;
        }

        /// <summary>
        ///     Adds the payload format indicator to the message.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="payloadFormatIndicator">
        ///     The payload format indicator.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
        public MqttApplicationMessageBuilder WithPayloadFormatIndicator(MqttPayloadFormatIndicator payloadFormatIndicator)
        {
            _payloadFormatIndicator = payloadFormatIndicator;
            return this;
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
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="responseTopic">
        ///     The response topic.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
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
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="subscriptionIdentifier">
        ///     The subscription identifier.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
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
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="topicAlias">
        ///     The topic alias.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
        public MqttApplicationMessageBuilder WithTopicAlias(ushort topicAlias)
        {
            _topicAlias = topicAlias;
            return this;
        }

        /// <summary>
        ///     Adds the user property to the message.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        /// <param
        ///     name="name">
        ///     The property name.
        /// </param>
        /// <param
        ///     name="value">
        ///     The property value.
        /// </param>
        /// <returns>
        ///     A new instance of the
        ///     <see
        ///         cref="MqttApplicationMessageBuilder" />
        ///     class.
        /// </returns>
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