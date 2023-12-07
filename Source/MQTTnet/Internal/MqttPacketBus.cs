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

        readonly AsyncSignal _signal = new AsyncSignal();
        readonly object _syncRoot = new object();

        int _activePartition = (int)MqttPacketBusPartition.Health;

        public int TotalItemsCount
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
                        // Iterate through the partitions in order to ensure processing of health packets
                        // even if lots of data packets are enqueued.

                        // Partition | Messages (left = oldest).
                        // DATA      | [#]#########################
                        // CONTROL   | [#]#############
                        // HEALTH    | [#]####

                        // In this sample the 3 oldest messages from the partitions are processed in a row.
                        // Then the next 3 from all 3 partitions.

                        MoveActivePartition();

                        var activePartition = _partitions[_activePartition];

                        if (activePartition.First != null)
                        {
                            var item = activePartition.First;
                            activePartition.RemoveFirst();

                            return item.Value;
                        }
                    }
                }

                // No partition contains data so that we have to wait and put
                // the worker back to the thread pool.
                try
                {
                    await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // The cancelled token should "hide" the disposal of the signal.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException("MqttPacketBus is broken.");
        }

        public void Dispose()
        {
            _signal.Dispose();
        }

        public MqttPacketBusItem DropFirstItem(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                var partitionInstance = _partitions[(int)partition];

                if (partitionInstance.Count > 0)
                {
                    var firstItem = partitionInstance.First.Value;
                    partitionInstance.RemoveFirst();

                    return firstItem;
                }
            }

            return null;
        }

        public void EnqueueItem(MqttPacketBusItem item, MqttPacketBusPartition partition)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_syncRoot)
            {
                _partitions[(int)partition].AddLast(item);
                _signal.Set();
            }
        }

        public List<MqttPacket> ExportPackets(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                return _partitions[(int)partition].Select(i => i.Packet).ToList();
            }
        }

        public int ItemsCount(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                return _partitions[(int)partition].Count;
            }
        }

        public int PartitionItemsCount(MqttPacketBusPartition partition)
        {
            lock (_syncRoot)
            {
                return _partitions[(int)partition].Count;
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
    }
}