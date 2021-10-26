using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttUnsubscribeResult
    {
        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; } = new List<MqttUnsubscribeReasonCode>(128);
        
        public bool CloseConnection { get; set; }
    }
}