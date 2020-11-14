using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscribeResult
    {
        public List<MqttSubscribeReturnCode> ReturnCodes { get; } = new List<MqttSubscribeReturnCode>();

        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>();

        public bool CloseConnection { get; set; }
    }
}
