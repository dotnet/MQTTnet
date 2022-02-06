// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class ValidatingConnectionEventArgs : EventArgs
    {
        readonly MqttConnectPacket _connectPacket;
        readonly IMqttChannelAdapter _clientAdapter;

        public ValidatingConnectionEventArgs(MqttConnectPacket connectPacket, IMqttChannelAdapter clientAdapter)
        {
            _connectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));
            _clientAdapter = clientAdapter ?? throw new ArgumentNullException(nameof(clientAdapter));
        }

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId => _connectPacket.ClientId;

        public string Endpoint => _clientAdapter.Endpoint;

        public bool IsSecureConnection => _clientAdapter.IsSecureConnection;

        public X509Certificate2 ClientCertificate => _clientAdapter.ClientCertificate;

        public MqttProtocolVersion ProtocolVersion => _clientAdapter.PacketFormatterAdapter.ProtocolVersion;

        public string Username => _connectPacket.Username;

        public byte[] RawPassword => _connectPacket.Password;

        public string Password => Encoding.UTF8.GetString(RawPassword ?? PlatformAbstractionLayer.EmptyByteArray);

        /// <summary>
        /// Gets or sets a value indicating whether clean sessions are used or not.
        /// When a client connects to a broker it can connect using either a non persistent connection (clean session) or a persistent connection.
        /// With a non persistent connection the broker doesn't store any subscription information or undelivered messages for the client.
        /// This mode is ideal when the client only publishes messages.
        /// It can also connect as a durable client using a persistent connection.
        /// In this mode, the broker will store subscription information, and undelivered messages for the client.
        /// </summary>
        public bool? CleanSession => _connectPacket.CleanSession;

        /// <summary>
        /// Gets or sets the keep alive period.
        /// The connection is normally left open by the client so that is can send and receive data at any time.
        /// If no data flows over an open connection for a certain time period then the client will generate a PINGREQ and expect to receive a PINGRESP from the broker.
        /// This message exchange confirms that the connection is open and working.
        /// This period is known as the keep alive period. 
        /// </summary>
        public ushort? KeepAlivePeriod => _connectPacket.KeepAlivePeriod;

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties => _connectPacket.Properties?.UserProperties;

        /// <summary>
        /// Gets or sets the authentication data.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public byte[] AuthenticationData => _connectPacket.Properties?.AuthenticationData;

        /// <summary>
        /// Gets or sets the authentication method.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string AuthenticationMethod => _connectPacket.Properties?.AuthenticationMethod;

        public uint? MaximumPacketSize => _connectPacket.Properties?.MaximumPacketSize;

        /// <summary>
        /// Gets or sets the receive maximum.
        /// This gives the maximum length of the receive messages.
        /// </summary>
        public ushort? ReceiveMaximum => _connectPacket.Properties?.ReceiveMaximum;

        /// <summary>
        /// Gets or sets the topic alias maximum.
        /// This gives the maximum length of the topic alias.
        /// </summary>
        public ushort TopicAliasMaximum => _connectPacket.Properties?.TopicAliasMaximum ?? 0;

        /// <summary>
        /// Gets the request problem information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool? RequestProblemInformation => _connectPacket.Properties?.RequestProblemInformation;

        /// <summary>
        /// Gets the request response information.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool? RequestResponseInformation => _connectPacket.Properties?.RequestResponseInformation;

        /// <summary>
        /// Gets the session expiry interval.
        /// The time after a session expires when it's not actively used.
        /// </summary>
        public uint? SessionExpiryInterval => _connectPacket.Properties?.SessionExpiryInterval;

        /// <summary>
        /// Gets or sets the will delay interval.
        /// This is the time between the client disconnect and the time the will message will be sent.
        /// </summary>
        public uint? WillDelayInterval => _connectPacket.WillProperties?.WillDelayInterval;

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems { get; internal set; }
        
        /// <summary>
        /// Gets or sets the reason code. When a MQTTv3 client connects the enum value must be one which is
        /// also supported in MQTTv3. Otherwise the connection attempt will fail because not all codes can be
        /// converted properly.
        /// MQTTv5 only.
        /// </summary>
        public MqttConnectReasonCode ReasonCode { get; set; } = MqttConnectReasonCode.Success;

        /// <summary>
        /// Gets or sets the response user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> ResponseUserProperties { get; set; }

        /// <summary>
        /// Gets or sets the response authentication data.
        /// MQTTv5 only.
        /// </summary>
        public byte[] ResponseAuthenticationData { get; set; }

        /// <summary>
        /// Gets or sets the assigned client identifier.
        /// MQTTv5 only.
        /// </summary>
        public string AssignedClientIdentifier { get; set; }

        public string ReasonString { get; set; }
        
        /// <summary>
        /// Gets or sets the server reference. This can be used together with i.e. "Server Moved" to send
        /// a different server address to the client.
        /// MQTTv5 only.
        /// </summary>
        public string ServerReference { get; set; }
    }
}
