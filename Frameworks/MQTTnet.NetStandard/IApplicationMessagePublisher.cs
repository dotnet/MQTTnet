using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet
{
    public interface IApplicationMessagePublisher
    {
        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);

        Task PublishAsync(MqttApplicationMessage applicationMessage);
    }
}
