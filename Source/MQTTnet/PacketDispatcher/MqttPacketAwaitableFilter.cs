// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.PacketDispatcher;

public sealed class MqttPacketAwaitableFilter
{
    public required Type Type { get; init; }

    public ushort Identifier { get; init; }
}