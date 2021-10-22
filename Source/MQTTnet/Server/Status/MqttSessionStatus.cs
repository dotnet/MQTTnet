﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V5;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server.Status
{
    public sealed class MqttSessionStatus : IMqttSessionStatus
    {
        readonly MqttClientSession _session;

        public MqttSessionStatus(MqttClientSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }
        
        public event EventHandler Deleted
        {
            add => _session.Deleted += value;
            remove => _session.Deleted += value;
        }

        public string ClientId => _session.ClientId;

        // TODO: Fix!
        public long PendingApplicationMessagesCount => 0; // _session.ApplicationMessagesQueue.Count;

        public DateTime CreatedTimestamp => _session.CreatedTimestamp;

        public IDictionary<object, object> Items => _session.Items;
        
        public Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            
            // _session.ApplicationMessagesQueue.Enqueue(new MqttQueuedApplicationMessage
            // {
            //     ApplicationMessage = applicationMessage,
            //     IsDuplicate = false,
            //     IsRetainedMessage = false,
            //     SubscriptionQualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
            //     SenderClientId = null
            // });

            var publishPacketFactory = new MqttPublishPacketFactory();
            _session.EnqueuePacket(new MqttPacketBusItem(publishPacketFactory.Create(applicationMessage)));
            
            return PlatformAbstractionLayer.CompletedTask;
        }

        public Task DeliverApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            
            var publishPacketFactory = new MqttPublishPacketFactory();
            var packetBusItem = new MqttPacketBusItem(publishPacketFactory.Create(applicationMessage));
            _session.EnqueuePacket(packetBusItem);

            return packetBusItem.WaitForDeliveryAsync();
        }

        public Task ClearApplicationMessagesQueueAsync()
        {
            // TODO: Fix!
            //_session.ApplicationMessagesQueue.Clear();
            return Task.FromResult(0);
        }

        public Task DeleteAsync()
        {
            return _session.DeleteAsync();
        }
        
        public Task ClearPendingApplicationMessagesAsync()
        {
            return ClearApplicationMessagesQueueAsync();
        }
    }
}
