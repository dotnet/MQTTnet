using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MQTTnet.Buffers
{
    public static class MqttMemoryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            source.AsSpan(sourceIndex, length).CopyTo(destination.AsSpan(destinationIndex, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReadOnlySequence<byte> sequence, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            var offset = destinationIndex;
            foreach (var segment in sequence)
            {
                if (segment.Length < sourceIndex)
                {
                    sourceIndex -= segment.Length;
                    continue;
                }
                var targetLength = Math.Min(segment.Length - sourceIndex, length);
                segment.Span.Slice(sourceIndex, targetLength).CopyTo(destination.AsSpan(offset));
                offset += targetLength;
                length -= targetLength;
                if (length == 0)
                {
                    break;
                }
                sourceIndex = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySequence<byte> RentCopy(ReadOnlySequence<byte> sequence, int sourceIndex, int length)
        {
            ArrayPoolBufferSegment<byte> firstSegment = null;
            ArrayPoolBufferSegment<byte> nextSegment = null;

            var offset = sourceIndex;
            foreach (var segment in sequence)
            {
                if (segment.Length >= sourceIndex)
                {
                    sourceIndex -= segment.Length;
                    continue;
                }

                var targetLength = Math.Min(segment.Length - sourceIndex, length);
                if (firstSegment == null)
                {
                    firstSegment = ArrayPoolBufferSegment<byte>.Rent(targetLength);
                    nextSegment = firstSegment;
                }
                else
                {
                    nextSegment = nextSegment.RentAndAppend(targetLength);
                }

                segment.Span.Slice(sourceIndex, targetLength).CopyTo(nextSegment.Array().AsSpan());
                offset += targetLength;
                length -= targetLength;
                if (length == 0)
                {
                    break;
                }
                sourceIndex = 0;
            }

            if (firstSegment == null)
            {
                return ReadOnlySequence<byte>.Empty;
            }
            return new ReadOnlySequence<byte>(firstSegment, 0, nextSegment, nextSegment.Memory.Length);
        }

    }
}

