// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class CheckSubscriptionsResult
    {
        public static CheckSubscriptionsResult NotSubscribed { get; } = new CheckSubscriptionsResult();

        public bool IsSubscribed { get; set; }

        public bool RetainAsPublished { get; set; }
        
        public List<uint> SubscriptionIdentifiers { get; set; }
        
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
