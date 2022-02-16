// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Internal
{
    public sealed class MqttPacketBus : IDisposable
    {
        readonly LinkedList<MqttPacketBusItem>[] _partitions =
        {
            new LinkedList<MqttPacketBusItem>(),
            new LinkedList<MqttPacketBusItem>(),
            new LinkedList<MqttPacketBusItem>()
        };

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        readonly object _syncRoot = new object();

        int _activePartition = (int) MqttPacketBusPartition.Health;

        public int ItemsCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _partitions.Sum(p => p.Count);
                }
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                foreach (var partition in _partitions)
                {
                    partition.Clear();
                }
            }
        }

        public async Task<MqttPacketBusItem> DequeueItemAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        MoveActivePartition();

                        if (_partitions[_activePartition].Count > 0)
                        {
                            var item = _partitions[_activePartition].First;
                            _partitions[_activePartition].RemoveFirst();
                            return item.Value;
                        }
                    }
                }

                // No partition contains data so that we have to wait and put
                // the worker back to the thread pool.
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException("MqttPacketBus is broken.");
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public void DropFirstItem(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                var partitionInstance = _partitions[(int) partition];

                if (partitionInstance.Any())
                {
                    partitionInstance.RemoveFirst();
                }
            }
        }

        public void EnqueueItem(MqttPacketBusItem item, MqttPacketBusPartition partition)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_syncRoot)
            {
                _partitions[(int) partition].AddLast(item);
            }

            _semaphore.Release();
        }

        public List<MqttPacket> ExportPackets(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                return _partitions[(int) partition].Select(i => i.Packet).ToList();
            }
        }

        void MoveActivePartition()
        {
            if (_activePartition >= _partitions.Length - 1)
            {
                _activePartition = 0;
            }
            else
            {
                _activePartition++;
            }
        }

        public int PartitionItemsCount(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                return _partitions[(int) partition].Count;
            }
        }
    }
}