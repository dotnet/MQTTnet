using System;

namespace MQTTnet.Core.Client
{
    public interface IApplicationMessageReceiver
    {
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
    }
}
