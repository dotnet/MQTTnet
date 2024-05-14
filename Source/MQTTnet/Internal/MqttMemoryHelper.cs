using System;
using System.Runtime.CompilerServices;

namespace MQTTnet.Internal
{
    public static class MqttMemoryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int length)
        {
            source.AsSpan(sourceIndex, length).CopyTo(destination.AsSpan(destinationIndex, length));
        }
    }
}
