using System;

namespace MQTTnet.Packets.Properties
{
    public class CorrelationDataProperty : BinaryDataProperty
    {
        public CorrelationDataProperty(ArraySegment<byte> data) 
            : base((byte)PropertyID.CorrelationData, data)
        {
        }
    }
}
