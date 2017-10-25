using System;

namespace MQTTnet.Core.Diagnostics
{
    public sealed class MqttNetTraceMessagePublishedEventArgs : EventArgs
    {
        public MqttNetTraceMessagePublishedEventArgs(MqttNetTraceMessage traceMessage)
        {
            TraceMessage = traceMessage ?? throw new ArgumentNullException(nameof(traceMessage));
        }

        public MqttNetTraceMessage TraceMessage { get; }
    }
}
