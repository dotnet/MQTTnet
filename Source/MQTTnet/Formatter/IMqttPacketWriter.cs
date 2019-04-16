namespace MQTTnet.Formatter
{
    public interface IMqttPacketWriter
    {
        int Length { get; }

        void WriteWithLengthPrefix(string value);
        void Write(byte returnCode);
        void WriteWithLengthPrefix(byte[] payload);
        void Write(ushort keepAlivePeriod);

        void Write(IMqttPacketWriter propertyWriter);
        void WriteVariableLengthInteger(uint length);
        void Write(byte[] payload, int v, int length);
        void Reset(int v);
        void Seek(int v);
        void FreeBuffer();
        byte[] GetBuffer();
    }
}
