using System;

namespace MQTTnet.Core.Serializer
{
    public class ByteReader
    {
        private readonly int _source;
        private int _index;

        public ByteReader(int source)
        {
            _source = source;
        }

        public bool Read()
        {
            if (_index >= 8)
            {
                throw new InvalidOperationException("End of byte reached.");
            }

            var result = ((1 << _index) & _source) > 0;
            _index++;
            return result;
        }

        public byte Read(int count)
        {
            if (_index + count > 8)
            {
                throw new InvalidOperationException("End of byte will be reached.");
            }

            var result = 0;
            for (var i = 0; i < count; i++)
            {
                if (((1 << _index) & _source) > 0)
                {
                    result |= 1 << i;
                }

                _index++;
            }

            return (byte)result;
        }
    }
}
