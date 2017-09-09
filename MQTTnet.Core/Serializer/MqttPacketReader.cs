using System;
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
        private readonly MemoryStream _remainingData = new MemoryStream(1024);
        private readonly IMqttCommunicationChannel _source;

        private int _remainingLength;

        public MqttPacketReader(IMqttCommunicationChannel source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public MqttControlPacketType ControlPacketType { get; private set; }

        public byte FixedHeader { get; private set; }

        public bool EndOfRemainingData => _remainingData.Position == _remainingData.Length;

        public async Task ReadToEndAsync()
        {
            await ReadFixedHeaderAsync();
            await ReadRemainingLengthAsync().ConfigureAwait(false);

            if (_remainingLength == 0)
            {
                return;
            }

            var buffer = new byte[_remainingLength];
            await ReadFromSourceAsync(buffer).ConfigureAwait(false);
            
            _remainingData.Write(buffer, 0, buffer.Length);
            _remainingData.Position = 0;
        }

        public async Task<byte> ReadRemainingDataByteAsync()
        {
            return (await ReadRemainingDataAsync(1).ConfigureAwait(false))[0];
        }

        public async Task<ushort> ReadRemainingDataUShortAsync()
        {
            var buffer = await ReadRemainingDataAsync(2).ConfigureAwait(false);

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
            return await ReadRemainingDataAsync(length).ConfigureAwait(false);
        }

        public Task<byte[]> ReadRemainingDataAsync()
        {
            return ReadRemainingDataAsync(_remainingLength - (int)_remainingData.Position);
        }

        public async Task<byte[]> ReadRemainingDataAsync(int length)
        {
            var buffer = new byte[length];
            await _remainingData.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return buffer;
        }

        private async Task ReadRemainingLengthAsync()
        {
            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            do
            {
                encodedByte = await ReadStreamByteAsync().ConfigureAwait(false);
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is ivalid.");
                }
            } while ((encodedByte & 128) != 0);

            _remainingLength = value;
        }

        private Task ReadFromSourceAsync(byte[] buffer)
        {
            try
            {
                return _source.ReadAsync(buffer);
            }
            catch (Exception exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        private async Task<byte> ReadStreamByteAsync()
        {
            var buffer = new byte[1];
            await ReadFromSourceAsync(buffer).ConfigureAwait(false);
            return buffer[0];
        }

        private async Task ReadFixedHeaderAsync()
        {
            FixedHeader = await ReadStreamByteAsync().ConfigureAwait(false);

            var byteReader = new ByteReader(FixedHeader);
            byteReader.Read(4);
            ControlPacketType = (MqttControlPacketType)byteReader.Read(4);
        }

        public void Dispose()
        {
            _remainingData?.Dispose();
        }
    }
}
