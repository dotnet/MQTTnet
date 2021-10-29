using System;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet
{
    public interface IMqttApplicationMessageReceiver
    {
        /// <summary>
        /// Fired when an application message was received.
        /// </summary>
        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;
    }
}
