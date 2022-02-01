using System;
using System.Collections.Generic;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientOptions : IMqttClientOptions
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets or sets a value indicating whether clean sessions are used or not.
        /// When a client connects to a broker it can connect using either a non persistent connection (clean session) or a persistent connection.
        /// With a non persistent connection the broker doesn't store any subscription information or undelivered messages for the client.
        /// This mode is ideal when the client only publishes messages.
        /// It can also connect as a durable client using a persistent connection.
        /// In this mode, the broker will store subscription information, and undelivered messages for the client.
        /// </summary>
        public bool CleanSession { get; set; } = true;

        public IMqttClientCredentials Credentials { get; set; }

        public IMqttExtendedAuthenticationExchangeHandler ExtendedAuthenticationExchangeHandler { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public IMqttClientChannelOptions ChannelOptions { get; set; }

        public TimeSpan CommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the keep alive period.
        /// The connection is normally left open by the client so that is can send and receive data at any time.
        /// If no data flows over an open connection for a certain time period then the client will generate a PINGREQ and expect to receive a PINGRESP from the broker.
        /// This message exchange confirms that the connection is open and working.
        /// This period is known as the keep alive period. 
        /// </summary>
        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Gets or sets the last will message.
        /// In MQTT, you use the last will message feature to notify other clients about an ungracefully disconnected client.
        /// </summary>
        public MqttApplicationMessage WillMessage { get; set; }

        /// <summary>
        /// Gets or sets the will delay interval.
        /// This is the time between the client disconnect and the time the will message will be sent.
        /// </summary>
        public uint? WillDelayInterval { get; set; }

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
        /// Gets or sets the receive maximum.
        /// This gives the maximum length of the receive messages.
        /// </summary>
        public ushort? ReceiveMaximum { get; set; }

        /// <summary>
        /// Gets or sets the request problem information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool RequestProblemInformation { get; set; } = true;

        /// <summary>
        /// Gets or sets the request response information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool RequestResponseInformation { get; set; }

        /// <summary>
        /// Gets or sets the session expiry interval.
        /// The time after a session expires when it's not actively used.
        /// </summary>
        public uint SessionExpiryInterval { get; set; }

        /// <summary>
        /// Gets or sets the topic alias maximum.
        /// This gives the maximum length of the topic alias.
        /// </summary>
        public ushort TopicAliasMaximum { get; set; }

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
