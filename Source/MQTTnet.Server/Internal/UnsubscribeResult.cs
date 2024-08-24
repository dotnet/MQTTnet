// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class UnsubscribeResult
    {
        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; } = new List<MqttUnsubscribeReasonCode>(128);

        public bool CloseConnection { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}