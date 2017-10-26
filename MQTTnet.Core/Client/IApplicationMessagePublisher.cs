using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Client
{
    public interface IApplicationMessagePublisher
    {
        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);
    }
}
