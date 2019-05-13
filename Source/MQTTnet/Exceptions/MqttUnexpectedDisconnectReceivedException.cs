using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Exceptions
{
    public class MqttUnexpectedDisconnectReceivedException : MqttCommunicationException
    {
        public MqttUnexpectedDisconnectReceivedException(MqttDisconnectPacket disconnectPacket) 
            : base($"Unexpected DISCONNECT (Reason code={disconnectPacket.ReasonCode}) received.")
        {
            ReasonCode = disconnectPacket.ReasonCode;
            SessionExpiryInterval = disconnectPacket.Properties?.SessionExpiryInterval;
            ReasonString = disconnectPacket.Properties?.ReasonString;
            ServerReference = disconnectPacket.Properties?.ServerReference;
            UserProperties = disconnectPacket.Properties?.UserProperties;
        }

        public MqttDisconnectReasonCode? ReasonCode { get; }

        public uint? SessionExpiryInterval { get; }

        public string ReasonString { get; }

        public List<MqttUserProperty> UserProperties { get; }

        public string ServerReference { get; }
    }
}
