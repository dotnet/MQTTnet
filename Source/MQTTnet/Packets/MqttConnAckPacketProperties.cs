using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnAckPacketProperties
    {
        public uint? SessionExpiryInterval { get; set; }

        public ushort? ReceiveMaximum { get; set; }

        public MqttQualityOfServiceLevel? MaximumQoS { get; set; }

        public bool? RetainAvailable { get; set; }

        public uint? MaximumPacketSize { get; set; }

        public string AssignedClientIdentifier { get; set; }

        public ushort? TopicAliasMaximum { get; set; }

        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public bool? WildcardSubscriptionAvailable { get; set; }

        public bool? SubscriptionIdentifiersAvailable { get; set; }

        public bool? SharedSubscriptionAvailable { get; set; }

        public ushort? ServerKeepAlive { get; set; }

        public string ResponseInformation { get; set; }

        public string ServerReference { get; set; }

        public string AuthenticationMethod { get; set; }

        public byte[] AuthenticationData { get; set; }
    }
}