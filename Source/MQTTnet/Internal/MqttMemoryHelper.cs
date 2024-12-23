using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MQTTnet.Internal
{
    public static class MqttMemoryHelper
    {
        public static ReadOnlyMemory<byte> Join(this ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return default;
            }

            if (buffer.IsSingleSegment)
            {
                return buffer.First;
            }

            return buffer.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            source.AsSpan(sourceIndex, length).CopyTo(destination.AsSpan(destinationIndex, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReadOnlySequence<byte> sequence, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            sequence.Slice(sourceIndex).CopyTo(destination.AsSpan(destinationIndex, length));
        }

        public static bool SequenceEqual(ArraySegment<byte> source, ArraySegment<byte> target)
        {
            return source.AsSpan().SequenceEqual(target);
        }

        public static bool SequenceEqual(ReadOnlySequence<byte> source, ReadOnlySequence<byte> target)
        {
            if (source.Length != target.Length)
            {
                return false;
            }

            long comparedLength = 0;
            long length = source.Length;

            int sourceOffset = 0;
            int targetOffset = 0;

            var sourceEnumerator = source.GetEnumerator();
            var targetEnumerator = target.GetEnumerator();

            ReadOnlyMemory<byte> sourceSegment = sourceEnumerator.Current;
            ReadOnlyMemory<byte> targetSegment = targetEnumerator.Current;

            while (true)
            {
                int compareLength = Math.Min(sourceSegment.Length - sourceOffset, targetSegment.Length - targetOffset);

                if (compareLength > 0 &&
                    !sourceSegment.Span.Slice(sourceOffset, compareLength).SequenceEqual(targetSegment.Span.Slice(targetOffset, compareLength)))
                {
                    return false;
                }

                comparedLength += compareLength;
                if (comparedLength >= length)
                {
                    return true;
                }

                sourceOffset += compareLength;
                if (sourceOffset >= sourceSegment.Length)
                {
                    if (!sourceEnumerator.MoveNext())
                    {
                        return false;
                    }

                    sourceSegment = sourceEnumerator.Current;
                    sourceOffset = 0;
                }

                targetOffset += compareLength;
                if (targetOffset >= targetSegment.Length)
                {
                    if (!targetEnumerator.MoveNext())
                    {
                        return false;
                    }

                    targetSegment = targetEnumerator.Current;
                    targetOffset = 0;
                }
            }
        }
    }
}
