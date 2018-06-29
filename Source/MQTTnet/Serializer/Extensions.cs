using System;

namespace MQTTnet.Serializer
{
    public static class Extensions
    {
        public static byte[] ToArray(this ArraySegment<byte> source)
        {
            if (source.Array == null)
            {
                return null;
            }

            var buffer = new byte[source.Count];
            if (buffer.Length > 0)
            {
                Array.Copy(source.Array, source.Offset, buffer, 0, buffer.Length);
            }

            return buffer;
        }
    }
}
