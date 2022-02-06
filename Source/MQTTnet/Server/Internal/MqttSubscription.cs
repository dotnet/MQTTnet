// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttSubscription
    {
        public string Topic { get; set; }
        
        public bool NoLocal { get; set; }
        
        public MqttRetainHandling RetainHandling { get; set; }
        
        public bool RetainAsPublished { get; set; }
        
        public MqttQualityOfServiceLevel GrantedQualityOfServiceLevel { get; set; }
        
        public uint Identifier { get; set; }
    }
}