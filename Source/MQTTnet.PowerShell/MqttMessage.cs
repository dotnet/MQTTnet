// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.PowerShell;

public class MqttMessage
{
    public required string? ContentType { get; init; }
    public required string Payload { get; init; }
    public int QoS { get; init; }
    public required byte[] RawPayload { get; init; }
    public string? ResponseTopic { get; init; }
    public bool Retain { get; init; }
    public required string Topic { get; init; }
    public required List<MqttUserProperty>? UserProperties { get; init; }
}