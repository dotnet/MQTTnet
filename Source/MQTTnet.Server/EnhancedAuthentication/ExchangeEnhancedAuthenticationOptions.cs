// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Server.EnhancedAuthentication;

public sealed class ExchangeEnhancedAuthenticationOptions
{
    public byte[] AuthenticationData { get; set; }

    public string ReasonString { get; set; }

    public List<MqttUserProperty> UserProperties { get; set; }
}