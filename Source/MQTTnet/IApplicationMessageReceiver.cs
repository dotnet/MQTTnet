using MQTTnet.Client.Receiving;

namespace MQTTnet
{
    public interface IApplicationMessageReceiver
    {
        IMqttApplicationMessageHandler ApplicationMessageReceivedHandler { get; set; }
    }
}
