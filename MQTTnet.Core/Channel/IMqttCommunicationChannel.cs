using System.Threading.Tasks;
using MQTTnet.Core.Client;
using System.IO;

namespace MQTTnet.Core.Channel
{
    public interface IMqttCommunicationChannel
    {
        Stream SendStream { get; }
        Stream ReceiveStream { get; }
        Stream RawReceiveStream { get; }

        Task ConnectAsync(MqttClientOptions options);
        Task DisconnectAsync();
    }
}
