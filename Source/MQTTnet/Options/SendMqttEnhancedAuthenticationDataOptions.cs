// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet;

public sealed class SendMqttEnhancedAuthenticationDataOptions
{
    public byte[] Data { get; init; }

    public string ReasonString { get; init; }

    public List<MqttUserProperty> UserProperties { get; init; }
}