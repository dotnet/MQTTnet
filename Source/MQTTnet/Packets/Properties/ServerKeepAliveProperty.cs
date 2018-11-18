using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ServerKeepAliveProperty : TwoByteIntegerProperty
    {
        public ServerKeepAliveProperty(ushort value) 
            : base((byte)MqttMessagePropertyID.ServerKeepAlive, value)
        {
        }
    }
}
