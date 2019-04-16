using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace MQTTnet.Benchmarks.Configurations
{
    /// <summary>
    /// this options may be used to run benchmarks in debugmode and attach a performance profiler
    /// https://benchmarkdotnet.org/Configs/Configs.htm
    /// </summary>
    public class AllowNonOptimized : BaseConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS
            Add(Job.InProcess);
        }
    }
}