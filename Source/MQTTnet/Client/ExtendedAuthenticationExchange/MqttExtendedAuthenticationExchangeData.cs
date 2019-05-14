using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.ExtendedAuthenticationExchange
{
    public class MqttExtendedAuthenticationExchangeData
    {
        public MqttAuthenticateReasonCode ReasonCode { get; set; }

        public string ReasonString { get; set; }

        public byte[] AuthenticationData { get; set; }

        public List<MqttUserProperty> UserProperties { get; }
    }
}
