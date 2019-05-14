using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.ExtendedAuthenticationExchange
{
    public class MqttExtendedAuthenticationExchangeContext
    {
        public MqttExtendedAuthenticationExchangeContext(MqttAuthPacket authPacket, IMqttClient client)
        {
            if (authPacket == null) throw new ArgumentNullException(nameof(authPacket));

            ReasonCode = authPacket.ReasonCode;
            ReasonString = authPacket.Properties?.ReasonString;
            AuthenticationMethod = authPacket.Properties?.AuthenticationMethod;
            AuthenticationData = authPacket.Properties?.AuthenticationData;
            UserProperties = authPacket.Properties?.UserProperties;

            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public MqttAuthenticateReasonCode ReasonCode { get; }

        public string ReasonString { get; }

        public string AuthenticationMethod { get; }

        public byte[] AuthenticationData { get; }

        public List<MqttUserProperty> UserProperties { get; }

        public IMqttClient Client { get; }
    }
}
