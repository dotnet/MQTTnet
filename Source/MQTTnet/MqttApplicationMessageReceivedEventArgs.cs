using System;
using System.Threading.Tasks;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessageReceivedEventArgs(string clientId, MqttApplicationMessage applicationMessage)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        }

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

        public Task<bool> PendingTask { get; set; }
    }
}
