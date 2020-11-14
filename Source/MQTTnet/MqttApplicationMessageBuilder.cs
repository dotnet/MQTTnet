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
        private MqttQualityOfServiceLevel _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
        private string _topic;
        private byte[] _payload;
        private bool _retain;
        private string _contentType;
        private string _responseTopic;
        private byte[] _correlationData;
        private ushort? _topicAlias;
        private List<uint> _subscriptionIdentifiers;
        private uint? _messageExpiryInterval;
        private MqttPayloadFormatIndicator? _payloadFormatIndicator;
        private List<MqttUserProperty> _userProperties;

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

            _payload = payload as byte[];

            if (_payload == null)
            {
                _payload = payload.ToArray();
            }

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
        /// This is only supported when using MQTTv5.
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

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public MqttApplicationMessageBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public MqttApplicationMessageBuilder WithResponseTopic(string responseTopic)
        {
            _responseTopic = responseTopic;
            return this;
        }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public MqttApplicationMessageBuilder WithCorrelationData(byte[] correlationData)
        {
            _correlationData = correlationData;
            return this;
        }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public MqttApplicationMessageBuilder WithTopicAlias(ushort topicAlias)
        {
            _topicAlias = topicAlias;
            return this;
        }

        /// <summary>
        /// This is only supported when using MQTTv5.
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
        /// This is only supported when using MQTTv5.
        /// </summary>
        public MqttApplicationMessageBuilder WithMessageExpiryInterval(uint messageExpiryInterval)
        {
            _messageExpiryInterval = messageExpiryInterval;
            return this;
        }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
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
    }
}
