using System;

namespace MQTTnet.Serializer
{
    public sealed class ByteWriter
    {
        private int _index;
        private int _byte;

        public byte Value => (byte)_byte;

        public void Write(int @byte, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var value = ((1 << i) & @byte) > 0;
                Write(value);
            }
        }

        public void Write(bool bit)
        {
            if (_index >= 8)
            {
                throw new InvalidOperationException("End of the byte reached.");
            }

            if (bit)
            {
                _byte |= 1 << _index;
            }

            _index++;
        }
    }
}
