namespace MQTTnet.Diagnostics
{
    public static class TargetFrameworkProvider
    {
        public static string TargetFramework
        {
            get
            {
#if NET452
                return "net452";
#elif NET461
                return "net461";
#elif NET472
                return "net472";
#elif NETSTANDARD1_3
                return "netstandard1.3";
#elif NETSTANDARD2_0
                return "netstandard2.0";
#elif NETSTANDARD2_1
                return "netstandard2.1";
#elif WINDOWS_UWP
                return "uap10.0";
#elif NETCOREAPP3_1
                return "netcoreapp3.1";
#elif NET5_0
                return "net5.0";
#elif NET6_0
                return "net6.0";
#endif
            }
        }
    }
}