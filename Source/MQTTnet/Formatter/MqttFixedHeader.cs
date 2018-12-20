namespace MQTTnet.Formatter
{
    public struct MqttFixedHeader
    {
        public MqttFixedHeader(byte flags, int remainingLength, int totalLength)
        {
            Flags = flags;
            RemainingLength = remainingLength;
            TotalLength = totalLength;
        }

        public byte Flags { get; }

        public int RemainingLength { get; }

        public int TotalLength { get; }
    }
}
