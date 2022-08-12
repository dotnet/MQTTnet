using System;

namespace MQTTnet.Internal
{
    internal static class Stopwatch
    {
        private const long TicksPerSecond = 10000000;
        private static readonly double _tickFrequency = (double)TicksPerSecond / System.Diagnostics.Stopwatch.Frequency;

        public static TimeSpan GetTimestamp()
            => TimeSpan.FromTicks((long)(System.Diagnostics.Stopwatch.GetTimestamp() * _tickFrequency));
    }
}