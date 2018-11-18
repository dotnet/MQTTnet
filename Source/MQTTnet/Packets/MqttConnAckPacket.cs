using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttConnAckPacket : MqttBasePacket
    {
        public MqttConnectReturnCode ConnectReturnCode { get; set; }

        /// <summary>
        /// Added in MQTTv3.1.1.
        /// </summary>
        public bool IsSessionPresent { get; set; }

        #region Added in MQTTv5

        public uint? SessionExpiryIntervalProperty { get; set; }

        // TODO: Add enum
        public byte? ReceiveMaximumProperty { get; set; }

        public bool? RetainAvailableProperty { get; set; }

        public uint? MaximumPacketSizeProperty { get; set; }

        public string AssignedClientIdentifierProperty { get; set; }

        public ushort? TopicAliasMaximumProperty { get; set; }

        public string ReasonStringProperty { get; set; }

        public List<MqttUserProperty> UserPropertiesProperty { get; set; }

        public bool? WildcardSubscriptionAvailableProperty { get; set; }

        public bool? SubscriptionIdentifiersAvailableProperty { get; set; }

        public bool? SharedSubscriptionAvailableProperty { get; set; }

        public ushort? ServerKeepAliveProperty { get; set; }

        public string ResponseInformationProperty { get; set; }

        public string ServerReferenceProperty { get; set; }

        public string AuthenticationMethodProperty { get; set; }

        public byte[] AuthenticationDataProperty { get; set; }

        #endregion

        public override string ToString()
        {
            return "ConnAck: [ConnectReturnCode=" + ConnectReturnCode + "] [IsSessionPresent=" + IsSessionPresent + "]";
        }
    }
}
