using System;

namespace MQTTnet
{
    public interface IApplicationMessageReceiver
    {
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
    }
}
