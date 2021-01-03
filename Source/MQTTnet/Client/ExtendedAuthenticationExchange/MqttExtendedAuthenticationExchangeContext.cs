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

        /// <summary>
        /// Gets the reason code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttAuthenticateReasonCode ReasonCode { get; }

        /// <summary>
        /// Gets the reason string.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; }

        /// <summary>
        /// Gets the authentication method.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string AuthenticationMethod { get; }

        /// <summary>
        /// Gets the authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public byte[] AuthenticationData { get; }

        /// <summary>
        /// Gets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        public IMqttClient Client { get; }
    }
}
