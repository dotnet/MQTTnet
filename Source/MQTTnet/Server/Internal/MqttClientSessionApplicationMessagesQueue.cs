using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSessionApplicationMessagesQueue : IDisposable
    {
        readonly AsyncQueue<MqttQueuedApplicationMessage> _messageQueue = new AsyncQueue<MqttQueuedApplicationMessage>();

        readonly IMqttServerOptions _options;

        public MqttClientSessionApplicationMessagesQueue(IMqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int Count => _messageQueue.Count;
        
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
                        _messageQueue.TryDequeue();
                    }
                }

                _messageQueue.Enqueue(queuedApplicationMessage);
            }
        }

        public async Task<MqttQueuedApplicationMessage> Dequeue(CancellationToken cancellationToken)
        {
            var dequeueResult = await _messageQueue.TryDequeueAsync(cancellationToken).ConfigureAwait(false);
            if (!dequeueResult.IsSuccess)
            {
                return null;
            }

            return dequeueResult.Item;
        }
        
        public void Clear()
        {
            _messageQueue.Clear();
        }
        
        public void Dispose()
        {
            _messageQueue?.Dispose();
        }
    }
}
