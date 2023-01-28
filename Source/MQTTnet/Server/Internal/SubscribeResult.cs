// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class SubscribeResult
    {
        public SubscribeResult(int topicsCount)
        {
            ReasonCodes = new List<MqttSubscribeReasonCode>(topicsCount);
            RetainedMessages = new List<MqttRetainedMessageMatch>();
        }

        public bool CloseConnection { get; set; }

        public List<MqttSubscribeReasonCode> ReasonCodes { get; set; }

        public string ReasonString { get; set; }

        public List<MqttRetainedMessageMatch> RetainedMessages { get; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}