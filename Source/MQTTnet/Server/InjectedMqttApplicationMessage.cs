// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace MQTTnet.Server
{
    public sealed class InjectedMqttApplicationMessage
    {
        public InjectedMqttApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        }
        
        public string SenderClientId { get; set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }
    }
}