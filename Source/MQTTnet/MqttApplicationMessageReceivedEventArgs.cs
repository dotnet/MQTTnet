using MQTTnet.Client;
using MQTTnet.Implementations;
using MQTTnet.Packets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessageReceivedEventArgs(MqttClient client, MqttPublishPacket publishPacket, CancellationToken cancellationToken, string clientId, MqttApplicationMessage applicationMessage)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            PublishPacket = publishPacket ?? throw new ArgumentNullException(nameof(publishPacket));
            CancellationToken = cancellationToken;
            acknowledged = 0;
            ClientId = clientId;
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            AutoAcknowledge = true;
        }

        public MqttApplicationMessageReceivedEventArgs(string clientId, MqttApplicationMessage applicationMessage)
        {
            acknowledged = 1;
            ClientId = clientId;
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            AutoAcknowledge = true;
        }

        internal MqttClient Client { get; }

        internal CancellationToken CancellationToken { get; }

        internal MqttPublishPacket PublishPacket { get; }

        int acknowledged;

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; }

        public bool ProcessingFailed { get; set; }

        public MqttApplicationMessageReceivedReasonCode ReasonCode { get; set; } = MqttApplicationMessageReceivedReasonCode.Success;
        
        /// <summary>
        /// Gets or sets whether this message was handled.
        /// This value can be used in user code for custom control flow.
        /// </summary>
        public bool IsHandled { get; set; }
        
        public object Tag { get; set; }

        public bool AutoAcknowledge { get; set; }

        public Task Acknowledge() 
        { 
            if (Interlocked.CompareExchange(ref acknowledged, 1, 0) == 0)
            {
                return Client.AcknowledgeReceivedPublishPacket(this);
            }
            else
            {
                return PlatformAbstractionLayer.CompletedTask;
            }
        }
    }
}
