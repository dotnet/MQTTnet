// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server;

public sealed class PreparingSessionEventArgs : EventArgs
{
    public string Id { get; set; }

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

    public IDictionary<object, object> Items { get; set; }

    public List<MqttPublishPacket> PublishPackets { get; } = new List<MqttPublishPacket>();

    DateTime? SessionExpiryTimestamp { get; set; }

    public List<MqttSubscription> Subscriptions { get; } = new List<MqttSubscription>();

    /// <summary>
    ///     Gets the will delay interval.
    ///     This is the time between the client disconnect and the time the will message will be sent.
    /// </summary>
    public uint? WillDelayInterval { get; set; }

    // <summary>
    //     Gets the last will message.
    //     In MQTT, you use the last will message feature to notify other clients about an ungracefully disconnected client.
    // </summary>
    // TODO: Use single properties. No entire will message.
    //MqttApplicationMessage WillMessage { get; set; }
}