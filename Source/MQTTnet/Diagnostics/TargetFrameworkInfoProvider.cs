namespace MQTTnet.Diagnostics
{
    public static class TargetFrameworkInfoProvider
    {
        public static string TargetFramework
        {
            get
            {
#if NET452
                return "net452";
#elif NETSTANDARD1_3
                return "netstandard1.3";
#elif NETSTANDARD2_0
                return "netstandard2.0";
#endif
            }
        }
    }
}