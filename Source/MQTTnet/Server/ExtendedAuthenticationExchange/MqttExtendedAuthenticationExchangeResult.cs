// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class MqttExtendedAuthenticationExchangeResult
    {
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public byte[] AuthenticationData { get; set; }
    }
}
