// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public sealed class ReceiveMqttEnhancedAuthenticationDataResult
{
    public byte[]? AuthenticationData { get; init; }

    public string? AuthenticationMethod { get; init; }

    public MqttAuthenticateReasonCode ReasonCode { get; init; }

    public string? ReasonString { get; init; }

    public List<MqttUserProperty>? UserProperties { get; init; }
}