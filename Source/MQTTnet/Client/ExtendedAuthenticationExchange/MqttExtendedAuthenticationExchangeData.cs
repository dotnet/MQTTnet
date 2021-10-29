using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttExtendedAuthenticationExchangeData
    {
        /// <summary>
        /// Gets or sets the reason code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttAuthenticateReasonCode ReasonCode { get; set; }

        /// <summary>
        /// Gets or sets the reason string.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; set; }

        /// <summary>
        /// Gets or sets the authentication data.
        /// Authentication data is binary information used to transmit multiple iterations of cryptographic secrets of protocol steps.
        /// The content of the authentication data is highly dependent on the specific implementation of the authentication method.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public byte[] AuthenticationData { get; set; }

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; }
    }
}
