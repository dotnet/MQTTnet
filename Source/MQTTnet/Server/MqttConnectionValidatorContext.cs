using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttConnectionValidatorContext
    {
        private readonly MqttConnectPacket _connectPacket;
        private readonly IMqttChannelAdapter _clientAdapter;

        public MqttConnectionValidatorContext(MqttConnectPacket connectPacket, IMqttChannelAdapter clientAdapter, IDictionary<object, object> sessionItems)
        {
            _connectPacket = connectPacket;
            _clientAdapter = clientAdapter ?? throw new ArgumentNullException(nameof(clientAdapter));
            SessionItems = sessionItems;
        }

        public string ClientId => _connectPacket.ClientId;

        public string Endpoint => _clientAdapter.Endpoint;

        public bool IsSecureConnection => _clientAdapter.IsSecureConnection;

        public X509Certificate2 ClientCertificate => _clientAdapter.ClientCertificate;

        public MqttProtocolVersion ProtocolVersion => _clientAdapter.PacketFormatterAdapter.ProtocolVersion;

        public string Username => _connectPacket?.Username;

        public byte[] RawPassword => _connectPacket?.Password;

        public string Password => Encoding.UTF8.GetString(RawPassword ?? new byte[0]);

        public MqttApplicationMessage WillMessage => _connectPacket?.WillMessage;

        public bool? CleanSession => _connectPacket?.CleanSession;

        public ushort? KeepAlivePeriod => _connectPacket?.KeepAlivePeriod;

        public List<MqttUserProperty> UserProperties => _connectPacket?.Properties?.UserProperties;

        public byte[] AuthenticationData => _connectPacket?.Properties?.AuthenticationData;

        public string AuthenticationMethod => _connectPacket?.Properties?.AuthenticationMethod;

        public uint? MaximumPacketSize => _connectPacket?.Properties?.MaximumPacketSize;

        public ushort? ReceiveMaximum => _connectPacket?.Properties?.ReceiveMaximum;

        public ushort? TopicAliasMaximum => _connectPacket?.Properties?.TopicAliasMaximum;

        public bool? RequestProblemInformation => _connectPacket?.Properties?.RequestProblemInformation;

        public bool? RequestResponseInformation => _connectPacket?.Properties?.RequestResponseInformation;

        public uint? SessionExpiryInterval => _connectPacket?.Properties?.SessionExpiryInterval;

        public uint? WillDelayInterval => _connectPacket?.Properties?.WillDelayInterval;

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; }

        /// <summary>
        /// This is used for MQTTv3 only.
        /// </summary>
        [Obsolete("Use ReasonCode instead. It is MQTTv5 only but will be converted to a valid ReturnCode.")]
        public MqttConnectReturnCode ReturnCode
        {
            get => new MqttConnectReasonCodeConverter().ToConnectReturnCode(ReasonCode);
            set => ReasonCode = new MqttConnectReasonCodeConverter().ToConnectReasonCode(value);
        }

        /// <summary>
        /// This is used for MQTTv5 only. When a MQTTv3 client connects the enum value must be one which is
        /// also supported in MQTTv3. Otherwise the connection attempt will fail because not all codes can be
        /// converted properly.
        /// </summary>
        public MqttConnectReasonCode ReasonCode { get; set; } = MqttConnectReasonCode.Success;

        public List<MqttUserProperty> ResponseUserProperties { get; set; }

        public byte[] ResponseAuthenticationData { get; set; }

        public string AssignedClientIdentifier { get; set; }

        public string ReasonString { get; set; }
    }
}
