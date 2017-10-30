using System;
using MQTTnet.Core.Client;

namespace MQTTnet.Core
{
    public interface IApplicationMessageReceiver
    {
        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
    }
}
