using System;
using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class AuthenticationDataProperty : BinaryDataProperty
    {
        public AuthenticationDataProperty(ArraySegment<byte> data) 
            : base((byte)MqttMessagePropertyID.AuthenticationData, data)
        {
        }
    }
}
