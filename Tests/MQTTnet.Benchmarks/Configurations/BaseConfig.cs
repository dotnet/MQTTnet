using BenchmarkDotNet.Configs;
using System.Linq;

namespace MQTTnet.Benchmarks.Configurations
{
    public class BaseConfig : ManualConfig
    {
        public BaseConfig()
        {
            AddLogger(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            AddExporter(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }
}
