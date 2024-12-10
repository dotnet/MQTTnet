// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class ClientSubscribedTopicEventArgs : EventArgs
    {
        public ClientSubscribedTopicEventArgs(string clientId, string userName, MqttTopicFilter topicFilter, IDictionary sessionItems)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            UserName = userName;
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        /// <summary>
        ///     Gets the client identifier.
        ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the user name of the client.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems { get; }

        /// <summary>
        ///     Gets the topic filter.
        ///     The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; }
    }
}