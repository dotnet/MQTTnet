using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Publishing
{
    public sealed class MqttClientPublishResultFactory
    {
        public MqttClientPublishResult Create(MqttPubAckPacket pubAckPacket)
        {
            var result = new MqttClientPublishResult
            {
                ReasonCode = MqttClientPublishReasonCode.Success,
            };

            if (pubAckPacket != null)
            {
                result.ReasonString = pubAckPacket.Properties.ReasonString;
                result.UserProperties.AddRange(pubAckPacket.Properties.UserProperties);
                
                // QoS 0 has no response. So we treat it as a success always.
                // Both enums have the same values. So it can be easily converted.
                result.ReasonCode = (MqttClientPublishReasonCode) (int) pubAckPacket.ReasonCode;

                result.PacketIdentifier = pubAckPacket.PacketIdentifier;
            }

            return result;
        }

        public MqttClientPublishResult Create(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
        {
            if (pubRecPacket == null || pubCompPacket == null)
            {
                return new MqttClientPublishResult
                {
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError
                };
            }

            MqttClientPublishResult result;
            
            // The PUBCOMP is the last packet in QoS 2. So we use the results from that instead of PUBREC.
            if (pubCompPacket.ReasonCode == MqttPubCompReasonCode.PacketIdentifierNotFound)
            {
                result = new MqttClientPublishResult
                {
                    PacketIdentifier = pubCompPacket.PacketIdentifier,
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError,
                    ReasonString = pubCompPacket.Properties.ReasonString
                };
                
                result.UserProperties.AddRange(pubCompPacket.Properties.UserProperties);
                return result;
            }

            result = new MqttClientPublishResult
            {
                PacketIdentifier = pubCompPacket.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success,
                ReasonString = pubCompPacket.Properties.ReasonString
            };
            
            result.UserProperties.AddRange(pubCompPacket.Properties.UserProperties);

            if (pubRecPacket.ReasonCode != MqttPubRecReasonCode.Success)
            {
                // Both enums share the same values.
                result.ReasonCode = (MqttClientPublishReasonCode) pubRecPacket.ReasonCode;
            }

            return result;
        }
    }
}