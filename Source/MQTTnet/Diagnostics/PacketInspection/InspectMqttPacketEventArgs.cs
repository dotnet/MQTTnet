using System;

namespace MQTTnet.Diagnostics
{
    public sealed class InspectMqttPacketEventArgs : EventArgs
    {
        public MqttPacketFlowDirection Direction { get; internal set; }

        public byte[] Buffer { get; set; }
    }
}