using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Connecting
{
    // TODO: Consider renaming this to _MqttClientConnectResult_
    public class MqttClientAuthenticateResult
    {
        public MqttClientConnectResultCode ResultCode { get; set; }

        public bool IsSessionPresent { get; set; }

        public bool? WildcardSubscriptionAvailable { get; set; }

        public bool? RetainAvailable { get; set; }

        public string AssignedClientIdentifier { get; set; }

        public string AuthenticationMethod { get; set; }

        public byte[] AuthenticationData { get; set; }

        public uint? MaximumPacketSize { get; set; }

        public string ReasonString { get; set; }

        public ushort? ReceiveMaximum { get; set; }
        
        public MqttQualityOfServiceLevel MaximumQoS { get; set; }

        public string ResponseInformation { get; set; }

        public ushort? TopicAliasMaximum { get; set; }

        public string ServerReference { get; set; }

        public ushort? ServerKeepAlive { get; set; }

        public uint? SessionExpiryInterval { get; set; }

        public bool? SubscriptionIdentifiersAvailable { get; set; }

        public bool? SharedSubscriptionAvailable { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
