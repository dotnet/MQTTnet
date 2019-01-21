using System;
using MQTTnet.Client.Receiving;

namespace MQTTnet
{
    public interface IApplicationMessageReceiver
    {
        IMqttApplicationMessageHandler ReceivedApplicationMessageHandler { get; set; }

        [Obsolete("Use _ReceivedApplicationMessageHandler_ instead.")]
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
    }
}
