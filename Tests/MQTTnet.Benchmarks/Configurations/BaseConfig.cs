using BenchmarkDotNet.Configs;
using System.Linq;

namespace MQTTnet.Benchmarks.Configurations
{
    public class BaseConfig : ManualConfig
    {
        public BaseConfig()
        {
            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }
}
