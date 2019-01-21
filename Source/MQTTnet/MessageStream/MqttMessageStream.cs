using System;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.MessageStream
{
    public class MqttMessageStream
    {
        private readonly LinkedList<MqttBasePacket> _queue = new LinkedList<MqttBasePacket>();
        private readonly LinkedList<TaskCompletionSource<MqttBasePacket>> _waitHandles = new LinkedList<TaskCompletionSource<MqttBasePacket>>();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private readonly BlockingQueue<MqttBasePacket> _packets = new BlockingQueue<MqttBasePacket>();

        public void Enqueue(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            

            lock (_queue)
            {
                //_queue.AddLast(packet);

                _packets.Enqueue(packet);
            }

            //lock (_queue)
            //{
            //    _queue.AddLast(packet);

            //    foreach (var waitHandle in _waitHandles)
            //    {
            //        waitHandle.TrySetResult(true);
            //    }

            //    _waitHandles.Clear();
            //}
        }
        
        public Task<MqttBasePacket> TakeAsync(CancellationToken cancellationToken)
        {
            lock (_packets)
            {
                var packet = _packets.Dequeue();
                return Task.FromResult(packet);
            }
            

            //while (!cancellationToken.IsCancellationRequested)
            //{
            //    TaskCompletionSource<bool> waitHandle;
            //    lock (_queue)
            //    {
            //        if (_queue.Count > 0)
            //        {
            //            var node = _queue.First;
            //            _queue.RemoveFirst();

            //            return node.Value;
            //        }

            //        waitHandle = new TaskCompletionSource<bool>();
            //        _waitHandles.Add(waitHandle);
            //    }

            //    await waitHandle.Task;
            //}

            //return null;
        }
    }
}
