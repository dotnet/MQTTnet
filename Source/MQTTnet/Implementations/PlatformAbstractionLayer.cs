using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public static class PlatformAbstractionLayer
    {
        public static Task CompletedTask
        {
            get
            {
#if NET452 
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
            }
        }

    }
}
