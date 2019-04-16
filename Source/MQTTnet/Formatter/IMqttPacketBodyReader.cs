using System;

namespace MQTTnet.Formatter
{
    public interface IMqttPacketBodyReader
    {
        ulong Length { get; }

        ulong Offset { get; }

        bool EndOfStream { get; }

        byte ReadByte();

        byte[] ReadRemainingData();

        ushort ReadTwoByteInteger();

        string ReadStringWithLengthPrefix();

        byte[] ReadWithLengthPrefix();

        uint ReadFourByteInteger();

        uint ReadVariableLengthInteger();

        bool ReadBoolean();

        void Seek(ulong position);
    }
}
