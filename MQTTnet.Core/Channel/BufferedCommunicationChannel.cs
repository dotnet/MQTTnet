using System.Threading.Tasks;
using MQTTnet.Core.Client;
using System;

namespace MQTTnet.Core.Channel
{
    public class BufferedCommunicationChannel : IMqttCommunicationChannel
    {
        private readonly IMqttCommunicationChannel _inner;
        private int _bufferSize;
        private int _bufferOffset;

        public BufferedCommunicationChannel(IMqttCommunicationChannel inner)
        {
            _inner = inner;
        }

        public Task ConnectAsync(MqttClientOptions options)
        {
            return _inner.ConnectAsync(options);
        }

        public Task DisconnectAsync()
        {
            return _inner.DisconnectAsync();
        }

        public int Peek()
        {
            return _inner.Peek();
        }

        public async Task<ArraySegment<byte>> ReadAsync(int length, byte[] buffer)
        {
            //read from buffer
            if (_bufferSize > 0)
            {
                return ReadFomBuffer(length, buffer);
            }

            var available = _inner.Peek();
            // if there are less or equal bytes available then requested then just read em
            if (available <= length)
            {
                return await _inner.ReadAsync(length, buffer);
            }

            //if more bytes are available than requested do buffer them to reduce calls to network buffers
            await WriteToBuffer(available, buffer).ConfigureAwait(false);
            return ReadFomBuffer(length, buffer);
        }

        private async Task WriteToBuffer(int available, byte[] buffer)
        {
            await _inner.ReadAsync(available, buffer).ConfigureAwait(false);
            _bufferSize = available;
            _bufferOffset = 0;
        }

        private ArraySegment<byte> ReadFomBuffer(int length, byte[] buffer)
        {
            var result = new ArraySegment<byte>(buffer, _bufferOffset, length);
            _bufferSize -= length;
            _bufferOffset += length;

            if (_bufferSize < 0)
            {
            }
            return result;
        }

        public Task WriteAsync(byte[] buffer)
        {
            return _inner.WriteAsync(buffer);
        }
    }
}
