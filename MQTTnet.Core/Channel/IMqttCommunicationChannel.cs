using System.Threading.Tasks;
using System.IO;

namespace MQTTnet.Core.Channel
{
    public interface IMqttCommunicationChannel
    {
        Stream SendStream { get; }
        Stream ReceiveStream { get; }
        Stream RawReceiveStream { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
    }
}
