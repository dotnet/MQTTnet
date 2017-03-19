using System;

namespace MQTTnet.Core.Serializer
{
    public class ByteReader
    {
        private int _index;
        private readonly int _byte;

        public ByteReader(byte @byte)
        {
            _byte = @byte;
        }

        public bool Read()
        {
            if (_index >= 8)
            {
                throw new InvalidOperationException("End of the byte reached.");
            }

            var result = ((1 << _index) & _byte) > 0;
            _index++;

            return result;
        }

        public byte Read(int count)
        {
            var result = 0;
            for (var i = 0; i < count; i++)
            {
                if (Read())
                {
                    result |= 1 << i;
                }
            }

            return (byte)result;
        }
    }
}
