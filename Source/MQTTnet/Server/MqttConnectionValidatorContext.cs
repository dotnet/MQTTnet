using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttConnectionValidatorContext : MqttBaseInterceptorContext
    {
        private readonly MqttConnectPacket _connectPacket;
        private readonly IMqttChannelAdapter _clientAdapter;

        public MqttConnectionValidatorContext(MqttConnectPacket connectPacket, IMqttChannelAdapter clientAdapter) : base(connectPacket, new ConcurrentDictionary<object, object>())
        {
            _connectPacket = connectPacket;
            _clientAdapter = clientAdapter ?? throw new ArgumentNullException(nameof(clientAdapter));
        }

        public string ClientId => _connectPacket.ClientId;

        public string Endpoint => _clientAdapter.Endpoint;

        public bool IsSecureConnection => _clientAdapter.IsSecureConnection;

        public X509Certificate2 ClientCertificate => _clientAdapter.ClientCertificate;

        public MqttProtocolVersion ProtocolVersion => _clientAdapter.PacketFormatterAdapter.ProtocolVersion;
        
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
