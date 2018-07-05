using MQTTnet.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class AsyncQueue<T> : IDisposable
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly AsyncAutoResetEvent _queueEmptyEvent = new AsyncAutoResetEvent();
        private readonly AsyncAutoResetEvent _queueNotFullEvent = new AsyncAutoResetEvent();
        protected readonly IMqttServerOptions _options;


        public AsyncQueue(IMqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }
        

        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
            }
        }

        public void Dispose()
        {
        }

        public void Enqueue(T packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            TryEnqueue(packet);
        }

        protected virtual bool TryEnqueue(T packet)
        {
            bool wait = false;
            lock (_queue)
            {
                if (_queue.Count >= _options.MaxPendingMessagesPerClient)
                {
                    switch (_options.PendingMessagesOverflowStrategy)
                    {
                        case MqttPendingMessagesOverflowStrategy.DropNewMessage:
                            return false;
                        case MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage:
                            _queue.Dequeue();
                            break;
                        case MqttPendingMessagesOverflowStrategy.Block:
                            wait = true;
                            break;
                    }
                }

                _queue.Enqueue(packet);
            }

            if (wait)
            {
                _queueNotFullEvent.WaitOneAsync().GetAwaiter().GetResult();
                _queue.Enqueue(packet);
            }

            _queueEmptyEvent.Set();
            return true;
        }

        public bool TryDequeue(out T packet)
        {
            packet = default(T);
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    packet = _queue.Dequeue();
                    _queueNotFullEvent.Set();
                    return true;
                }
                return false;
            }
        }

        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queueEmptyEvent.WaitOneAsync(cancellationToken).ContinueWith(t =>
            {
                if (!t.IsCanceled && !t.IsFaulted && TryDequeue(out var packet))
                {
                    return packet;
                }

                return default(T);
            });
        }
    }
}
