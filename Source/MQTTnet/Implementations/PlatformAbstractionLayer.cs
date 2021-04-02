using System;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public static class PlatformAbstractionLayer
    {
#if NET452
        public static Task CompletedTask => Task.FromResult(0);

        public static byte[] EmptyByteArray { get; } = new byte[0];
#else
        public static Task CompletedTask => Task.CompletedTask;

        public static byte[] EmptyByteArray { get; } = Array.Empty<byte>();
#endif

        public static void Sleep(TimeSpan timeout)
        {
#if !NETSTANDARD1_3
            System.Threading.Thread.Sleep(timeout);
#else
            Task.Delay(timeout).Wait();
#endif
        }
    }
}
