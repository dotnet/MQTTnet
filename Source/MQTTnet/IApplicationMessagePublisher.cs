using System.Threading.Tasks;

namespace MQTTnet
{
    public interface IApplicationMessagePublisher
    {
        Task PublishAsync(MqttApplicationMessage applicationMessage);
    }
}
