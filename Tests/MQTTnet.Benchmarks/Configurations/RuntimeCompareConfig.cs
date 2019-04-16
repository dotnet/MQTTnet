using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace MQTTnet.Benchmarks.Configurations
{
    public class RuntimeCompareConfig : BaseConfig
    {
        public RuntimeCompareConfig()
        {
            Add(Job.Default.With(Runtime.Clr));
            Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp21));
        }

    }
}
