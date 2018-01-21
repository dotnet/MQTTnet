using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    public sealed class MqttPacketReader : BinaryReader
    {
        private readonly MqttPacketHeader _header;
        
        public MqttPacketReader(MqttPacketHeader header, Stream bodyStream)
            : base(bodyStream, Encoding.UTF8, true)
        {
            _header = header;
        }

        public bool EndOfRemainingData => BaseStream.Position == _header.BodyLength;

        public static async Task<MqttPacketHeader> ReadHeaderFromSourceAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] singleByteBuf = new byte[1];
            var readCount = await stream.ReadAsync(singleByteBuf, 0, singleByteBuf.Length).ConfigureAwait(false);
            if (readCount <= 0)
            {
                return null;
            }

            var fixedHeader = singleByteBuf[0];
            var controlPacketType = (MqttControlPacketType)(fixedHeader >> 4);
            var bodyLength = await ReadBodyLengthFromSourceAsync(stream, cancellationToken).ConfigureAwait(false);

            return new MqttPacketHeader
            {
                FixedHeader = fixedHeader,
                ControlPacketType = controlPacketType,
                BodyLength = bodyLength
            };
        }

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
            if (buffer.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public byte[] ReadWithLengthPrefix()
        {
            var length = ReadUInt16();
            if (length == 0)
            {
                return new byte[0];
            }

            return ReadBytes(length);
        }

        public byte[] ReadRemainingData()
        {
            return ReadBytes(_header.BodyLength - (int)BaseStream.Position);
        }

        private static async Task<int> ReadBodyLengthFromSourceAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;

            byte[] singleByteBuf = new byte[1];

            var readBytes = new List<byte>();
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                int readCount = await stream.ReadAsync(singleByteBuf, 0, singleByteBuf.Length).ConfigureAwait(false);
                if (readCount <= 0)
                {
                    throw new MqttCommunicationException("Connection closed while reading remaining length data.");
                }

                encodedByte = singleByteBuf[0];
                readBytes.Add(encodedByte);

                value += (byte)(encodedByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException($"Remaining length is invalid (Data={string.Join(",", readBytes)}).");
                }
            } while ((encodedByte & 128) != 0);

            return value;
        }
    }
}
