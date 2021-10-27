using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server
{
    public sealed class PreparingMqttClientSessionEventArgs : EventArgs
    {
        public string ClientId { get; internal set; }
        
        // TODO: Allow adding of packets to the queue etc.
        
        /*
         * The Session State in the Server consists of:
·         The existence of a Session, even if the rest of the Session State is empty.
·         The Clients subscriptions, including any Subscription Identifiers.
·         QoS 1 and QoS 2 messages which have been sent to the Client, but have not been completely acknowledged.
·         QoS 1 and QoS 2 messages pending transmission to the Client and OPTIONALLY QoS 0 messages pending transmission to the Client.
·         QoS 2 messages which have been received from the Client, but have not been completely acknowledged.The Will Message and the Will Delay Interval
·         If the Session is currently not connected, the time at which the Session will end and Session State will be discarded.
         */
        
        public bool IsExistingSession { get; set; }

        public List<MqttSubscription> Subscriptions { get; } = new List<MqttSubscription>();

        public List<MqttPublishPacket> PublishPackets { get; } = new List<MqttPublishPacket>();
    }
}