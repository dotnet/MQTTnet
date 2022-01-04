using System;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPublishPacket : MqttBasePacket, IMqttPacketWithIdentifier, IDisposable
    {
        [ThreadStatic]
        private static MqttPublishPacket t_cache;

        public ushort PacketIdentifier { get; set; }

        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        #region Added in MQTTv5

        public MqttPublishPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("Publish: [Topic=", Topic, "] [Payload.Length=", Payload?.Length, "] [QoSLevel=", QualityOfServiceLevel, "] [Dup=", Dup, "] [Retain=", Retain, "] [PacketIdentifier=", PacketIdentifier, "]");
        }

        void IDisposable.Dispose()
        {
            PacketIdentifier = default;
            Retain = default;
            QualityOfServiceLevel = default;
            Dup = default;
            Topic = null;
            Payload = null;

            ((IDisposable)Properties).Dispose();
            Properties = null;

            t_cache = this;
        }

        internal static MqttPublishPacket GetInstance()
        {
            return t_cache ?? new MqttPublishPacket();
        }
    }
}
