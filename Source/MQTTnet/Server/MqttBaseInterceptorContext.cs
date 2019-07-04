using System.Collections.Generic;
using System.Text;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttBaseInterceptorContext
    {
        private readonly MqttConnectPacket _connectPacket;

        protected MqttBaseInterceptorContext(MqttConnectPacket connectPacket, IDictionary<object, object> sessionItems)
        {
            _connectPacket = connectPacket;
            SessionItems = sessionItems;
        }

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
    }
}