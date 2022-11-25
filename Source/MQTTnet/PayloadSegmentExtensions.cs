using MQTTnet.Internal;
using System;
using System.Runtime.InteropServices;

namespace MQTTnet
{
    public static class PayloadSegmentExtensions
    {
        private static readonly ArraySegment<byte> emptySegment = new ArraySegment<byte>(EmptyBuffer.Array);

        /// <summary>
        /// Get the ArraySegment style of Payload
        /// </summary>
        /// <param name="segmentable"></param>
        /// <returns></returns>
        public static ArraySegment<byte> GetPayloadSegment(this IPayloadSegmentable segmentable)
        {
            if (segmentable.Payload == null)
            {
                return emptySegment;
            }

            var payloadLength = segmentable.PayloadLength == null
                ? segmentable.Payload.Length - segmentable.PayloadOffset
                : segmentable.PayloadLength.Value;

            return new ArraySegment<byte>(segmentable.Payload, segmentable.PayloadOffset, payloadLength);
        }

        /// <summary>
        /// Set payloadSegment to Payload, PayloadOffset and PayloadLength
        /// </summary>
        /// <param name="segmentable"></param>
        /// <param name="payloadSegment"></param>
        public static void SetPayloadSegment(this IPayloadSegmentable segmentable, ArraySegment<byte> payloadSegment)
        {
            segmentable.Payload = payloadSegment.Array;
            segmentable.PayloadOffset = payloadSegment.Offset;
            segmentable.PayloadLength = payloadSegment.Count;
        }

#if NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1
        /// <summary>
        /// Set payloadSegment to Payload, PayloadOffset and PayloadLength
        /// </summary>
        /// <param name="segmentable"></param>
        /// <param name="payloadSegment"></param>
        public static void SetPayloadSegment(this IPayloadSegmentable segmentable, ReadOnlyMemory<byte> payloadSegment)
        {
            if (MemoryMarshal.TryGetArray(payloadSegment, out var segment))
            {
                segmentable.SetPayloadSegment(segment);
            }
            else
            {
                segmentable.Payload = payloadSegment.ToArray();
                segmentable.PayloadOffset = 0;
                segmentable.PayloadLength = null;
            }
        }
#endif

    }
}