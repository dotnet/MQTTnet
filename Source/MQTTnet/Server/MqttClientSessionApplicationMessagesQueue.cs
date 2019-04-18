using MQTTnet.Internal;
using MQTTnet.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttClientSessionApplicationMessagesQueue : IDisposable
    {
        private readonly AsyncQueue<MqttQueuedApplicationMessage> _messageQueue = new AsyncQueue<MqttQueuedApplicationMessage>();
        
        private readonly IMqttServerOptions _options;

        public MqttClientSessionApplicationMessagesQueue(IMqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int Count => _messageQueue.Count;

        public void Enqueue(MqttApplicationMessage applicationMessage, string senderClientId, MqttQualityOfServiceLevel qualityOfServiceLevel, bool isRetainedMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            Enqueue(new MqttQueuedApplicationMessage
            {
                ApplicationMessage = applicationMessage,
                SenderClientId = senderClientId,
                QualityOfServiceLevel = qualityOfServiceLevel,
                IsRetainedMessage = isRetainedMessage
            });
        }

        public void Clear()
        {
            _messageQueue.Clear();
        }

        public ValueTask<MqttQueuedApplicationMessage> TakeAsync(CancellationToken cancellationToken)
        {
            return _messageQueue.DequeueAsync(cancellationToken);
        }

        public void Enqueue(MqttQueuedApplicationMessage queuedApplicationMessage)
        {
            if (queuedApplicationMessage == null) throw new ArgumentNullException(nameof(queuedApplicationMessage));

            lock (_messageQueue)
            {
                if (_messageQueue.Count >= _options.MaxPendingMessagesPerClient)
                {
                    if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                    {
                        return;
                    }

                    if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                    {
                        _messageQueue.TryDequeue(out _);
                    }
                }

                _messageQueue.Enqueue(queuedApplicationMessage);
            }
        }

        public void Dispose()
        {
            _messageQueue.Dispose();
        }
    }
}
