using System.Buffers;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class LimitedMemoryPool : MemoryPool<byte>
    {
        protected override void Dispose(bool disposing)
        {
        }

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            return new MemoryOwner(minBufferSize);
        }

        public override int MaxBufferSize { get; }
    }
}