using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Connecting
{
    // TODO: Consider renaming this to _MqttClientConnectResult_
    public class MqttClientAuthenticateResult
    {
        /// <summary>
        /// Gets or sets the result code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientConnectResultCode ResultCode { get; set; }

        public bool IsSessionPresent { get; set; }

        public bool? WildcardSubscriptionAvailable { get; set; }

        public bool? RetainAvailable { get; set; }

        public string AssignedClientIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the authentication method.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string AuthenticationMethod { get; set; }

        /// <summary>
        /// Gets or sets the authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public byte[] AuthenticationData { get; set; }

        public uint? MaximumPacketSize { get; set; }

        /// <summary>
        /// Gets or sets the reason string.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; set; }

        public ushort? ReceiveMaximum { get; set; }
        
        public MqttQualityOfServiceLevel MaximumQoS { get; set; }

        /// <summary>
        /// Gets or sets the response information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string ResponseInformation { get; set; }

        public ushort? TopicAliasMaximum { get; set; }

        public string ServerReference { get; set; }

        public ushort? ServerKeepAlive { get; set; }

        public uint? SessionExpiryInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription identifiers are available or not.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool? SubscriptionIdentifiersAvailable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shared subscriptions are available or not.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool? SharedSubscriptionAvailable { get; set; }

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
