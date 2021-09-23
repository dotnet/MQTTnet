using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class SubscribeResult
    {
        public List<MqttSubscribeReturnCode> ReturnCodes { get; } = new List<MqttSubscribeReturnCode>(128);

        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>(128);

        public List<MqttQueuedApplicationMessage> RetainedApplicationMessages { get; } = new List<MqttQueuedApplicationMessage>(1024);
        
        public bool CloseConnection { get; set; }
    }
}
