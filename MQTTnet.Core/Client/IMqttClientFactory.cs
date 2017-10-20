using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.ManagedClient;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient(IMqttNetTraceHandler traceHandler = null);

        ManagedMqttClient CreateManagedMqttClient(IMqttNetTraceHandler traceHandler = null);
    }
}