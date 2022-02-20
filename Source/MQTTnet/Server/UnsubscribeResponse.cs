// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class UnsubscribeResponse
    {
        /// <summary>
        /// Gets or sets the reason code which is sent to the client.
        /// MQTTv5 only.
        /// </summary>
        public MqttUnsubscribeReasonCode ReasonCode { get; set; }
        
        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
        
        public string ReasonString { get; set; }
    }
}