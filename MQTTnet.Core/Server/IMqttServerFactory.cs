using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Server
{
    public interface IMqttServerFactory
    {
        IMqttServer CreateMqttServer(MqttServerOptions options, IMqttNetTraceHandler traceHandler = null);
    }
}