using MQTTnet;
using MQTTnet.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MQTTnet
{
    static class PayloadSegmentExtensions
    {
        private static readonly ArraySegment<byte> empty = new ArraySegment<byte>(EmptyBuffer.Array);

        public static int? GetPayloadCount(this IPayloadSegmentable segmentable)
        {
            if (segmentable.Payload == null)
            {
                return null;
            }

            return segmentable.PayloadCount == null
                ? segmentable.Payload.Length - segmentable.PayloadOffset
                : segmentable.PayloadCount.Value;
        }

        public static ArraySegment<byte> GetPayloadSegment(this IPayloadSegmentable segmentable)
        {
            if (segmentable.Payload == null)
            {
                return empty;
            }

            var count = segmentable.PayloadCount == null
                ? segmentable.Payload.Length - segmentable.PayloadOffset
                : segmentable.PayloadCount.Value;

            return new ArraySegment<byte>(segmentable.Payload, segmentable.PayloadOffset, count);
        }

#if NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1
        public static bool SequenceEqual(this ArraySegment<byte> x, ArraySegment<byte> y)
        {
            return x.Count == y.Count && x.AsSpan().SequenceEqual(y.AsSpan());
        }
#else
        public static bool SequenceEqual(this ArraySegment<byte> x, ArraySegment<byte> y)
        {
            if (x.Count == y.Count)
            {
                var xValue = (IEnumerable<byte>)x;
                var yValue = (IEnumerable<byte>)y;
                return xValue.SequenceEqual(yValue);
            }
            return false;
        }
#endif
    }
}
