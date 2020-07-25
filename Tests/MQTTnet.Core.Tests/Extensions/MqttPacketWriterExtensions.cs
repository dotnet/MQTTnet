using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public static class MqttPacketWriterExtensions
    {
        public static byte[] AddMqttHeader(this IMqttPacketWriter writer, MqttControlPacketType header, byte[] body)
        {
            writer.Write(MqttPacketWriter.BuildFixedHeader(header));
            writer.WriteVariableLengthInteger((uint)body.Length);
            writer.Write(body, 0, body.Length);
            return writer.GetBuffer();
        }
    }
}
