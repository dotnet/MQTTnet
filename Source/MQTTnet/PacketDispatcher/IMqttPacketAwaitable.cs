// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher;

public interface IMqttPacketAwaitable : IDisposable
{
    MqttPacketAwaitableFilter Filter { get; }

    void Complete(MqttPacket packet);

    void Fail(Exception exception);

    void Cancel();
}