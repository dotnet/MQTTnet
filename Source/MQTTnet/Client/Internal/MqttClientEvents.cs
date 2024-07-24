// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Internal;

namespace MQTTnet.Client.Internal;

public sealed class MqttClientEvents
{
    public AsyncEvent<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedEvent { get; } = new();
    public AsyncEvent<MqttClientConnectedEventArgs> ConnectedEvent { get; } = new();
    public AsyncEvent<MqttClientConnectingEventArgs> ConnectingEvent { get; } = new();
    public AsyncEvent<MqttClientDisconnectedEventArgs> DisconnectedEvent { get; } = new();
    public AsyncEvent<InspectMqttPacketEventArgs> InspectPacketEvent { get; } = new();
}