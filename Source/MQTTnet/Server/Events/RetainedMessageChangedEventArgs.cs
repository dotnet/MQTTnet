// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class RetainedMessageChangedEventArgs : EventArgs
    {
        public string ClientId { get; internal set; }
        
        public MqttApplicationMessage ChangedRetainedMessage { get; internal set; }
        
        public List<MqttApplicationMessage> StoredRetainedMessages { get; internal set; }
    }
}