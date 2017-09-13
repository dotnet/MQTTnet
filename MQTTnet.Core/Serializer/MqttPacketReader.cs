using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketReader : BinaryReader
    {
        private readonly MqttPacketHeader _header;

        public MqttPacketReader(MqttPacketHeader header, Stream body)
            : base(body)
        {
            _header = header;
        }
        
        public bool EndOfRemainingData => BaseStream.Position == _header.BodyLength;
        
        public override ushort ReadUInt16()
        {
            var buffer = ReadBytes(2);

            var temp = buffer[0];
            buffer[0] = buffer[1];
            buffer[1] = temp;

            return BitConverter.ToUInt16(buffer, 0);
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public byte[] ReadWithLengthPrefix()
        {
            var length = ReadUInt16();
            return ReadBytes(length);
        }

        public byte[] ReadRemainingData()
        {
            return ReadBytes(_header.BodyLength - (int)BaseStream.Position);
        }

        public static async Task<MqttPacketHeader> ReadHeaderFromSourceAsync(IMqttCommunicationChannel source, byte[] buffer)
        {
            var fixedHeader = await ReadStreamByteAsync(source, buffer).ConfigureAwait(false);
            var byteReader = new ByteReader(fixedHeader);
            byteReader.Read(4);

            var controlPacketType = (MqttControlPacketType)byteReader.Read(4);
            var bodyLength = await ReadBodyLengthFromSourceAsync(source, buffer).ConfigureAwait(false);

            return new MqttPacketHeader
            {
                FixedHeader = fixedHeader,
                ControlPacketType = controlPacketType,
                BodyLength = bodyLength
            };
        }

        private static async Task<byte> ReadStreamByteAsync(IMqttCommunicationChannel source, byte[] readBuffer)
        {
            var result = await ReadFromSourceAsync(source, 1, readBuffer).ConfigureAwait(false);
            return result.Array[result.Offset];
        }

        public static async Task<ArraySegment<byte>> ReadFromSourceAsync(IMqttCommunicationChannel source, int length, byte[] buffer)
        {
            try
            {
                return await source.ReadAsync(length, buffer);
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }
        
        private static async Task<int> ReadBodyLengthFromSourceAsync(IMqttCommunicationChannel source, byte[] buffer)
        {
            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            do
            {
                encodedByte = await ReadStreamByteAsync(source, buffer).ConfigureAwait(false);
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is ivalid.");
                }
            } while ((encodedByte & 128) != 0);
            return value;
        }
    }
}
