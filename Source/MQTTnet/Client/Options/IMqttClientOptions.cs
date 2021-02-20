using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Client.Options
{
    public interface IMqttClientOptions
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Gets a value indicating whether clean sessions are used or not.
        /// When a client connects to a broker it can connect using either a non persistent connection (clean session) or a persistent connection.
        /// With a non persistent connection the broker doesn't store any subscription information or undelivered messages for the client.
        /// This mode is ideal when the client only publishes messages.
        /// It can also connect as a durable client using a persistent connection.
        /// In this mode, the broker will store subscription information, and undelivered messages for the client.
        /// </summary>
        bool CleanSession { get; }

        IMqttClientCredentials Credentials { get; }

        IMqttExtendedAuthenticationExchangeHandler ExtendedAuthenticationExchangeHandler { get; }

        MqttProtocolVersion ProtocolVersion { get; }

        IMqttClientChannelOptions ChannelOptions { get; }

        TimeSpan CommunicationTimeout { get; }

        /// <summary>
        /// Gets the keep alive period.
        /// The connection is normally left open by the client so that is can send and receive data at any time.
        /// If no data flows over an open connection for a certain time period then the client will generate a PINGREQ and expect to receive a PINGRESP from the broker.
        /// This message exchange confirms that the connection is open and working.
        /// This period is known as the keep alive period. 
        /// </summary>
        TimeSpan KeepAlivePeriod { get; }

        /// <summary>
        /// Gets the last will message.
        /// In MQTT, you use the last will message feature to notify other clients about an ungracefully disconnected client.
        /// </summary>
        MqttApplicationMessage WillMessage { get; }

        /// <summary>
        /// Gets the will delay interval.
        /// This is the time between the client disconnect and the time the will message will be sent.
        /// </summary>
        uint? WillDelayInterval { get; }

        /// <summary>
        /// Gets the authentication method.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        string AuthenticationMethod { get; }

        /// <summary>
        /// Gets the authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        byte[] AuthenticationData { get; }

        uint? MaximumPacketSize { get; }

        /// <summary>
        /// Gets the receive maximum.
        /// This gives the maximum length of the receive messages.
        /// </summary>
        ushort? ReceiveMaximum { get; }

        /// <summary>
        /// Gets the request problem information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        bool? RequestProblemInformation { get; }

        /// <summary>
        /// Gets the request response information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        bool? RequestResponseInformation { get; }

        /// <summary>
        /// Gets the session expiry interval.
        /// The time after a session expires when it's not actively used.
        /// </summary>
        uint? SessionExpiryInterval { get; }

        /// <summary>
        /// Gets the topic alias maximum.
        /// This gives the maximum length of the topic alias.
        /// </summary>
        ushort? TopicAliasMaximum { get; }

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        List<MqttUserProperty> UserProperties { get; set; }

        IMqttPacketInspector PacketInspector { get; set; }
    }
}