using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public interface IMqttClientRetainedMessageManager
    {
        Task LoadMessagesAsync();

        Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage);

        List<MqttApplicationMessage> GetMessages(MqttSubscribePacket subscribePacket);
    }
}
