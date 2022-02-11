// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class ClientSubscribedTopicEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; internal set; }
    }
}
