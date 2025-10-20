// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Exceptions;

public sealed class MqttClientUnexpectedDisconnectReceivedException(MqttDisconnectPacket disconnectPacket, Exception? innerException = null) : MqttCommunicationException(
    $"Unexpected DISCONNECT (Reason code={disconnectPacket.ReasonCode}) received.",
    innerException)
{
    public MqttDisconnectReasonCode? ReasonCode { get; } = disconnectPacket.ReasonCode;

    public string? ReasonString { get; } = disconnectPacket.ReasonString;

    public string? ServerReference { get; } = disconnectPacket.ServerReference;

    public uint? SessionExpiryInterval { get; } = disconnectPacket.SessionExpiryInterval;

    public List<MqttUserProperty>? UserProperties { get; } = disconnectPacket.UserProperties;
}