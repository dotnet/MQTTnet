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
            Add(Job.Default.With(ClrRuntime.Net472));
            Add(Job.Default.With(CoreRuntime.Core22).With(CsProjCoreToolchain.NetCoreApp22));
        }

    }
}
