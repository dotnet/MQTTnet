namespace MQTTnet.Serializer
{
    public struct MqttFixedHeader
    {
        public MqttFixedHeader(byte flags, int remainingLength)
        {
            Flags = flags;
            RemainingLength = remainingLength;
        }

        public byte Flags { get; }

        public int RemainingLength { get; }
    }
}
