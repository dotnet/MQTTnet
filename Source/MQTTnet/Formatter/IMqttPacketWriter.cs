namespace MQTTnet.Formatter
{
    public interface IMqttPacketWriter
    {
        int Length { get; }

        void WriteWithLengthPrefix(string value);

        void Write(byte value);

        void WriteWithLengthPrefix(byte[] value);

        void Write(ushort value);

        void Write(IMqttPacketWriter value);

        void WriteVariableLengthInteger(uint value);

        void Write(byte[] value, int offset, int length);

        void Reset(int length);

        void Seek(int offset);

        void FreeBuffer();

        byte[] GetBuffer();
    }
}
