using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;

namespace MQTTnet
{
    public interface IMqttApplicationMessagePublisher
    {
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default);
    }
}
