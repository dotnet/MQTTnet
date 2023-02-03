// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttSessionStatus
    {
        readonly MqttSession _session;

        public MqttSessionStatus(MqttSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public DateTime CreatedTimestamp => _session.CreatedTimestamp;

        public string Id => _session.Id;

        public IDictionary Items => _session.Items;

        public long PendingApplicationMessagesCount => _session.PendingDataPacketsCount;

        public Task ClearApplicationMessagesQueueAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync()
        {
            return _session.DeleteAsync();
        }

        public Task DeliverApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            var packetBusItem = new MqttPacketBusItem(MqttPacketFactories.Publish.Create(applicationMessage));
            _session.EnqueueDataPacket(packetBusItem);

            return packetBusItem.WaitAsync();
        }

        public Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            _session.EnqueueDataPacket(new MqttPacketBusItem(MqttPacketFactories.Publish.Create(applicationMessage)));

            return CompletedTask.Instance;
        }
    }
}