using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientQueuedStorage
    {
        Task SaveQueuedMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadQueuedMessagesAsync();
    }
}
