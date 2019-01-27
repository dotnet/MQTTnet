using MQTTnet.Internal;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttPendingApplicationMessage
    {
        public MqttApplicationMessage ApplicationMessage { get; set; }

        public string SenderClientId { get; set; }

        public bool IsRetainedMessage { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool IsDuplicate { get; set; }
    }

    public class MqttClientSessionApplicationMessagesQueue : IDisposable
    {
        private readonly Queue<MqttPendingApplicationMessage> _messageQueue = new Queue<MqttPendingApplicationMessage>();
        private readonly AsyncAutoResetEvent _messageQueueLock = new AsyncAutoResetEvent();

        private readonly IMqttServerOptions _options;

        public MqttClientSessionApplicationMessagesQueue(IMqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

        }

        public int Count
        {
            get
            {
                lock (_messageQueue)
                {
                    return _messageQueue.Count;
                }
            }
        }

        public void Enqueue(MqttApplicationMessage applicationMessage, string senderClientId, MqttQualityOfServiceLevel qualityOfServiceLevel, bool isRetainedMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            Enqueue(new MqttPendingApplicationMessage
            {
                ApplicationMessage = applicationMessage,
                SenderClientId = senderClientId,
                QualityOfServiceLevel = qualityOfServiceLevel,
                IsRetainedMessage = isRetainedMessage
            });
        }

        public void Clear()
        {
            lock (_messageQueue)
            {
                _messageQueue.Clear();
            }
        }

        public void Dispose()
        {
        }

        public async Task<MqttPendingApplicationMessage> TakeAsync(CancellationToken cancellationToken)
        {
            // TODO: Create a blocking queue from this.

            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_messageQueue)
                {
                    if (_messageQueue.Count > 0)
                    {
                        return _messageQueue.Dequeue();
                    }
                }

                await _messageQueueLock.WaitOneAsync(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public void Enqueue(MqttPendingApplicationMessage enqueuedApplicationMessage)
        {
            if (enqueuedApplicationMessage == null) throw new ArgumentNullException(nameof(enqueuedApplicationMessage));

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
                        _messageQueue.Dequeue();
                    }
                }

                _messageQueue.Enqueue(enqueuedApplicationMessage);
            }

            _messageQueueLock.Set();
        }
    }
}
