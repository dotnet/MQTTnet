using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;

namespace MQTTnet
{
    public interface IApplicationMessagePublisher
    {
        ValueTask<MqttClientPublishReasonCode> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken);
    }
}
