using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public class MqttApplicationMessageBuilder
    {
        /// <summary>
        /// The quality of service level.
        /// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message.
        /// There are 3 QoS levels in MQTT:
        /// - At most once  (0): Message gets delivered no time, once or multiple times.
        /// - At least once (1): Message gets delivered at least once (one time or more often).
        /// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        MqttQualityOfServiceLevel _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;

        /// <summary>
        /// The MQTT topic.
        /// In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected client.
        /// The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level separator). 
        /// </summary>
        string _topic;

        /// <summary>
        /// The payload.
        /// The payload is the data bytes sent via the MQTT protocol.
        /// </summary>
        byte[] _payload;

        /// <summary>
        /// A value indicating whether the message should be retained or not.
        /// A retained message is a normal MQTT message with the retained flag set to true.
        /// The broker stores the last retained message and the corresponding QoS for that topic.
        /// </summary>
        bool _retain;

        bool _dup;

        /// <summary>
        /// The content type.
        /// The content type must be a UTF-8 encoded string. The content type value identifies the kind of UTF-8 encoded payload.
        /// </summary>
        string _contentType;

        /// <summary>
        /// The response topic.
        /// In MQTT 5 the ability to publish a response topic was added in the publish message which allows you to implement the request/response pattern between clients that is common in web applications.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        string _responseTopic;

        /// <summary>
        /// The correlation data.
        /// In order for the sender to know what sent message the response refers to it can also send correlation data with the published message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        byte[] _correlationData;

        /// <summary>
        /// The topic alias.
        /// Topic aliases were introduced are a mechanism for reducing the size of published packets by reducing the size of the topic field.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        ushort? _topicAlias;

        /// <summary>
        /// The subscription identifiers.
        /// The client can specify a subscription identifier when subscribing.
        /// The broker will establish and store the mapping relationship between this subscription and subscription identifier when successfully create or modify subscription.
        /// The broker will return the subscription identifier associated with this PUBLISH packet and the PUBLISH packet to the client when need to forward PUBLISH packets matching this subscription to this client.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        List<uint> _subscriptionIdentifiers;

        /// <summary>
        /// The message expiry interval.
        /// A client can set the message expiry interval in seconds for each PUBLISH message individually.
        /// This interval defines the period of time that the broker stores the PUBLISH message for any matching subscribers that are not currently connected.
        /// When no message expiry interval is set, the broker must store the message for matching subscribers indefinitely.
        /// When the retained=true option is set on the PUBLISH message, this interval also defines how long a message is retained on a topic.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        uint? _messageExpiryInterval;

        /// <summary>
        /// The payload format indicator.
        /// The payload format indicator is part of any MQTT packet that can contain a payload. The indicator is an optional byte value.
        /// A value of 0 indicates an “unspecified byte stream”.
        /// A value of 1 indicates a "UTF-8 encoded payload".
        /// If no payload format indicator is provided, the default value is 0.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        MqttPayloadFormatIndicator? _payloadFormatIndicator;

        /// <summary>
        /// The user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// </summary>
        /// Hint: MQTT 5 feature only.
        List<MqttUserProperty> _userProperties;

        public MqttApplicationMessageBuilder WithTopic(string topic)
        {
            _topic = topic;
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
                payload.Read(_payload, 0, _payload.Length);
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

        public MqttApplicationMessageBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            return this;
        }

        public MqttApplicationMessageBuilder WithQualityOfServiceLevel(int qualityOfServiceLevel)
        {
            if (qualityOfServiceLevel < 0 || qualityOfServiceLevel > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(qualityOfServiceLevel));
            }

            return WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qualityOfServiceLevel);
        }

        public MqttApplicationMessageBuilder WithRetainFlag(bool value = true)
        {
            _retain = value;
            return this;
        }

        public MqttApplicationMessageBuilder WithDupFlag(bool value = true)
        {
            _dup = value;
            return this;
        }

        public MqttApplicationMessageBuilder WithAtLeastOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            return this;
        }

        public MqttApplicationMessageBuilder WithAtMostOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            return this;
        }

        public MqttApplicationMessageBuilder WithExactlyOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            return this;
        }

        /// <summary>
        /// Adds the user property to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithUserProperty(string name, string value)
        {
            if (_userProperties == null)
            {
                _userProperties = new List<MqttUserProperty>();
            }

            _userProperties.Add(new MqttUserProperty(name, value));
            return this;
        }

        /// <summary>
        /// Adds the content type to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        /// <summary>
        /// Adds the response topic to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="responseTopic">The response topic.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithResponseTopic(string responseTopic)
        {
            _responseTopic = responseTopic;
            return this;
        }

        /// <summary>
        /// Adds the correlation data to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="correlationData">The correlation data.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithCorrelationData(byte[] correlationData)
        {
            _correlationData = correlationData;
            return this;
        }

        /// <summary>
        /// Adds the topic alias to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="topicAlias">The topic alias.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithTopicAlias(ushort topicAlias)
        {
            _topicAlias = topicAlias;
            return this;
        }

        /// <summary>
        /// Adds the subscription identifier to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="subscriptionIdentifier">The subscription identifier.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
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
        /// Adds the message expiry interval in seconds to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="messageExpiryInterval">The message expiry interval.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithMessageExpiryInterval(uint messageExpiryInterval)
        {
            _messageExpiryInterval = messageExpiryInterval;
            return this;
        }

        /// <summary>
        /// Adds the payload format indicator to the message.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="payloadFormatIndicator">The payload format indicator.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttApplicationMessageBuilder WithPayloadFormatIndicator(MqttPayloadFormatIndicator payloadFormatIndicator)
        {
            _payloadFormatIndicator = payloadFormatIndicator;
            return this;
        }

        public MqttApplicationMessage Build()
        {
            if (!_topicAlias.HasValue && string.IsNullOrEmpty(_topic))
            {
                throw new MqttProtocolViolationException("Topic or TopicAlias is not set.");
            }

            if (_topicAlias == 0)
            {
                throw new MqttProtocolViolationException("A Topic Alias of 0 is not permitted. A sender MUST NOT send a PUBLISH packet containing a Topic Alias which has the value 0 [MQTT-3.3.2-8].");
            }

            if (_qualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce && _dup)
            {
                throw new MqttProtocolViolationException("The DUP flag MUST be set to 0 for all QoS 0 messages [MQTT-3.3.1-2].");
            }

            var applicationMessage = new MqttApplicationMessage
            {
                Topic = _topic,
                Payload = _payload,
                QualityOfServiceLevel = _qualityOfServiceLevel,
                Retain = _retain,
                Dup = _dup,
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
    }
}
