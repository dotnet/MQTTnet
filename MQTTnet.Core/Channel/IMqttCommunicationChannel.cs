using System.Threading.Tasks;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.Channel
{
    public interface IMqttCommunicationChannel
    {
        Task ConnectAsync(MqttClientOptions options);

        Task DisconnectAsync();

        Task WriteAsync(byte[] buffer);

        Task ReadAsync(byte[] buffer);
    }
}
