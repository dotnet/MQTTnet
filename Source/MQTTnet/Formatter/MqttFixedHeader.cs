namespace MQTTnet.Formatter
{
    public struct MqttFixedHeader
    {
        public MqttFixedHeader(byte flags, uint remainingLength)
        {
            Flags = flags;
            RemainingLength = remainingLength;
        }

        public byte Flags { get; }

        public uint RemainingLength { get; }
    }
}
