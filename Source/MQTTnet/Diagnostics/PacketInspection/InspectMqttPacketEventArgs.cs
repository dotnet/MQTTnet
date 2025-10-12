// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Diagnostics.PacketInspection;

public sealed class InspectMqttPacketEventArgs : EventArgs
{
    public InspectMqttPacketEventArgs(MqttPacketFlowDirection direction, byte[] buffer)
    {
        Direction = direction;
        Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public byte[] Buffer { get; }

    public MqttPacketFlowDirection Direction { get; }
}