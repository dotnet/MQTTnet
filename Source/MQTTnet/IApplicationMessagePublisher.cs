using System.Threading.Tasks;
using MQTTnet.Client.Publishing;

namespace MQTTnet
{
    public interface IApplicationMessagePublisher
    {
        ValueTask<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage);
    }
}
