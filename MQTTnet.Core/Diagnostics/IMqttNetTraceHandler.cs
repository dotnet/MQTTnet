namespace MQTTnet.Core.Diagnostics
{
    public interface IMqttNetTraceHandler
    {
        bool IsEnabled { get; }

        void HandleTraceMessage(MqttNetTraceMessage traceMessage);
    }
}
