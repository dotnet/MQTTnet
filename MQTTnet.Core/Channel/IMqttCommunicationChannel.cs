using System.Threading.Tasks;
using MQTTnet.Core.Client;
using System.IO;

namespace MQTTnet.Core.Channel
{
    public interface IMqttCommunicationChannel
    {
        Task ConnectAsync(MqttClientOptions options);

        Task DisconnectAsync();
        
        Stream SendStream { get; }

        Stream ReceiveStream { get; }

        Stream RawStream { get; }
    }
}
