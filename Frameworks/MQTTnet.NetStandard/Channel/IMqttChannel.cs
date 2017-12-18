using System.IO;
using System.Threading.Tasks;

namespace MQTTnet.Channel
{
    public interface IMqttChannel
    {
        Stream SendStream { get; }
        Stream ReceiveStream { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
    }
}
