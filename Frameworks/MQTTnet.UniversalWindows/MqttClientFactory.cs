using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttClientFactory : IMqttClientFactory
    {
        public IMqttClient CreateMqttClient(IMqttNetTraceHandler traceHandler = null)
        {
            return new MqttClient(new MqttCommunicationAdapterFactory(), new MqttNetTrace(traceHandler));
        }

        public ManagedMqttClient CreateManagedMqttClient(IMqttNetTraceHandler traceHandler = null)
        {
            return new ManagedMqttClient(new MqttCommunicationAdapterFactory(), new MqttNetTrace(traceHandler));
        }
    }
}