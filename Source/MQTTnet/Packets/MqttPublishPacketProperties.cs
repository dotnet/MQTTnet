using System;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPublishPacketProperties : IDisposable
    {
        [ThreadStatic]
        private static MqttPublishPacketProperties t_cache;

        public MqttPayloadFormatIndicator? PayloadFormatIndicator { get; set; }

        public uint? MessageExpiryInterval { get; set; }

        public ushort? TopicAlias { get; set; }

        public string ResponseTopic { get; set; }

        public byte[] CorrelationData { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public List<uint> SubscriptionIdentifiers { get; set; }

        public string ContentType { get; set; }

        void IDisposable.Dispose()
        {
            PayloadFormatIndicator = null;
            MessageExpiryInterval = null;
            TopicAlias = null;
            ResponseTopic = null;
            CorrelationData = null;
            UserProperties = null;
            SubscriptionIdentifiers = null;
            ContentType = null;
        }

        internal static MqttPublishPacketProperties GetInstance()
        {
            return t_cache ?? new MqttPublishPacketProperties();
        }
    }
}
