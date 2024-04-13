// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Client.Internal
{
    public sealed class MqttClientEvents
    {
        public AsyncEvent<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedEvent { get; } = new AsyncEvent<MqttApplicationMessageReceivedEventArgs>();
        public AsyncEvent<MqttClientConnectedEventArgs> ConnectedEvent { get; } = new AsyncEvent<MqttClientConnectedEventArgs>();
        public AsyncEvent<MqttClientConnectingEventArgs> ConnectingEvent { get; } = new AsyncEvent<MqttClientConnectingEventArgs>();
        public AsyncEvent<MqttClientDisconnectedEventArgs> DisconnectedEvent { get; } = new AsyncEvent<MqttClientDisconnectedEventArgs>();
        public AsyncEvent<MqttClientDisconnectingEventArgs> DisconnectingEvent { get; } = new AsyncEvent<MqttClientDisconnectingEventArgs>();
        public AsyncEvent<InspectMqttPacketEventArgs> InspectPacketEvent { get; } = new AsyncEvent<InspectMqttPacketEventArgs>();
    }
}