using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core
{
    public interface IApplicationMessagePublisher
    {
        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);
    }
}
