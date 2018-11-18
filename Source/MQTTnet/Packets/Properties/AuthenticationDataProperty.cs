using System;

namespace MQTTnet.Packets.Properties
{
    public class AuthenticationDataProperty : BinaryDataProperty
    {
        public AuthenticationDataProperty(ArraySegment<byte> data) 
            : base((byte)PropertyID.AuthenticationData, data)
        {
        }
    }
}
