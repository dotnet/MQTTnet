namespace MQTTnet.Formatter
{
    public interface IMqttPacketBodyReader
    {
        int Length { get; }

        int Offset { get; }

        bool EndOfStream { get; }

        byte ReadByte();

        byte[] ReadRemainingData();

        ushort ReadTwoByteInteger();

        string ReadStringWithLengthPrefix();

        byte[] ReadWithLengthPrefix();

        uint ReadFourByteInteger();

        uint ReadVariableLengthInteger();

        bool ReadBoolean();

        void Seek(int position);
    }
}
