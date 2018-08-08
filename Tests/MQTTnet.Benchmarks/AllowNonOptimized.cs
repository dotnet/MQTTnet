using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace MQTTnet.Benchmarks
{
    /// <summary>
    /// this options may be used to run benchmarks in debugmode and attach a performance profiler
    /// https://benchmarkdotnet.org/Configs/Configs.htm
    /// </summary>
    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS
            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
            Add(Job.InProcess);
        }
    }
}