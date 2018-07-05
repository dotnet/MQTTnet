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
        private readonly AsyncAutoResetEvent _queueAutoResetEvent = new AsyncAutoResetEvent();
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
            lock (_queue)
            {
                if (_queue.Count >= _options.MaxPendingMessagesPerClient)
                {
                    if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                    {
                        return false;
                    }

                    if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                    {
                        _queue.Dequeue();
                    }
                }

                _queue.Enqueue(packet);
            }

            _queueAutoResetEvent.Set();
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
                    return true;
                }
                return false;
            }
        }

        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queueAutoResetEvent.WaitOneAsync(cancellationToken).ContinueWith(t =>
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
