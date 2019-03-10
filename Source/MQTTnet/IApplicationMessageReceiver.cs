using MQTTnet.Client.Receiving;

namespace MQTTnet
{
    public interface IApplicationMessageReceiver
    {
        IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }
    }
}
