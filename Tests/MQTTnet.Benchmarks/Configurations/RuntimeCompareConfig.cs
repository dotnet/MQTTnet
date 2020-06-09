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
            AddJob(Job.Default.WithRuntime(ClrRuntime.Net472));
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core22).WithToolchain(CsProjCoreToolchain.NetCoreApp22));
        }

    }
}
