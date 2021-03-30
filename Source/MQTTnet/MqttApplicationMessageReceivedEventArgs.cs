using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        readonly Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task> _acknowledgeHandler;
        
        int _isAcknowledged;
        
        public MqttApplicationMessageReceivedEventArgs(
            string clientId, 
            MqttApplicationMessage applicationMessage,
            MqttPublishPacket publishPacket,
            Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task> acknowledgeHandler)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            PublishPacket = publishPacket;
            _acknowledgeHandler = acknowledgeHandler;
        }

        internal MqttPublishPacket PublishPacket { get; }
        
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

        /// <summary>
        /// Gets ir sets whether the library should send MQTT ACK packets automatically if required.
        /// </summary>
        public bool AutoAcknowledge { get; set; } = true;
        
        public object Tag { get; set; }
        
        public Task AcknowledgeAsync(CancellationToken cancellationToken) 
        {
            if (_acknowledgeHandler == null)
            {
                throw new NotSupportedException("Deferred acknowledgement of application message is not yet supported in MQTTnet server.");
            }
            
            if (Interlocked.CompareExchange(ref _isAcknowledged, 1, 0) == 0)
            {
                return _acknowledgeHandler(this, cancellationToken);
            }
            
            throw new InvalidOperationException("The application message is already acknowledged.");
        }
    }
}
