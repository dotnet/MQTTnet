using System;

namespace MQTTnet.Server
{
    public sealed class MqttInjectedApplicationMessage
    {
        public MqttInjectedApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        }
        
        public string SenderClientId { get; set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }
    }
}