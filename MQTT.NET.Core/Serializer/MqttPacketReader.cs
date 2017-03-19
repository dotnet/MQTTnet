using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketReader : IDisposable
    {
        private readonly MemoryStream _remainingData = new MemoryStream();
        private readonly IMqttTransportChannel _source;

        public MqttPacketReader(IMqttTransportChannel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            _source = source;
        }

        public MqttControlPacketType ControlPacketType { get; private set; }

        public byte FixedHeader { get; private set; }

        public int RemainingLength { get; private set; }

        public bool EndOfRemainingData => _remainingData.Position == _remainingData.Length;

        public async Task ReadToEndAsync()
        {
            await ReadFixedHeaderAsync();
            await ReadRemainingLengthAsync();

            if (RemainingLength == 0)
            {
                return;
            }

            var buffer = new byte[RemainingLength];
            await _source.ReadAsync(buffer);

            _remainingData.Write(buffer, 0, buffer.Length);
            _remainingData.Position = 0;
        }

        private async Task ReadFixedHeaderAsync()
        {
            FixedHeader = await ReadStreamByteAsync();

            var byteReader = new ByteReader(FixedHeader);
            byteReader.Read(4);
            ControlPacketType = (MqttControlPacketType)byteReader.Read(4);
        }

        private async Task<byte> ReadStreamByteAsync()
        {
            var buffer = new byte[1];
            await _source.ReadAsync(buffer);
            return buffer[0];
        }

        private async Task ReadRemainingLengthAsync()
        {
            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            do
            {
                encodedByte = await ReadStreamByteAsync();
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is ivalid.");
                }
            } while ((encodedByte & 128) != 0);

            RemainingLength = value;
        }

        public async Task<byte> ReadRemainingDataByteAsync()
        {
            return (await ReadRemainingDataAsync(1))[0];
        }

        public async Task<ushort> ReadRemainingDataUShortAsync()
        {
            var buffer = await ReadRemainingDataAsync(2);

            var temp = buffer[0];
            buffer[0] = buffer[1];
            buffer[1] = temp;

            return BitConverter.ToUInt16(buffer, 0);
        }

        public async Task<string> ReadRemainingDataStringWithLengthPrefixAsync()
        {
            var buffer = await ReadRemainingDataWithLengthPrefixAsync();
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public async Task<byte[]> ReadRemainingDataWithLengthPrefixAsync()
        {
            var length = await ReadRemainingDataUShortAsync();
            return await ReadRemainingDataAsync(length);
        }

        public async Task<byte[]> ReadRemainingDataAsync()
        {
            return await ReadRemainingDataAsync(RemainingLength - (int)_remainingData.Position);
        }

        public async Task<byte[]> ReadRemainingDataAsync(int length)
        {
            var buffer = new byte[length];
            await _remainingData.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }

        public void Dispose()
        {
            _remainingData?.Dispose();
        }
    }
}
