using System;
using System.Runtime.CompilerServices;

namespace MQTTnet.Internal
{
    public static class Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
#if NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1
            source.AsSpan(sourceIndex, length).CopyTo(destination.AsSpan(destinationIndex, length));
#elif NET461_OR_GREATER || NETSTANDARD1_3_OR_GREATER
            unsafe
            {
                fixed (byte* pSoure = &source[sourceIndex])
                {
                    fixed (byte* pDestination = &destination[destinationIndex])
                    {
                        System.Buffer.MemoryCopy(pSoure, pDestination, length, length);
                    }
                }
            }
#else
            Array.Copy(source, sourceIndex, destination, destinationIndex, length);
#endif
        }
    }
}
