using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Server
{
    public interface IMqttServerStorage
    {
        Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync();
    }
}
