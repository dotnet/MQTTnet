// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class InterceptingSubscriptionEventArgs : EventArgs
    {
        public InterceptingSubscriptionEventArgs(
            CancellationToken cancellationToken,
            string clientId,
            string userName,
            MqttSessionStatus session,
            MqttTopicFilter topicFilter,
            List<MqttUserProperty> userProperties)
        {
            CancellationToken = cancellationToken;
            ClientId = clientId;
            UserName = userName;
            Session = session;
            TopicFilter = topicFilter;
            UserProperties = userProperties;
        }

        /// <summary>
        ///     Gets the cancellation token which can indicate that the client connection gets down.
        /// </summary>
        public CancellationToken CancellationToken { get; }

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
        ///     Gets or sets whether the broker should close the client connection.
        /// </summary>
        public bool CloseConnection { get; set; }

        /// <summary>
        ///     Gets or sets whether the broker should create an internal subscription for the client.
        ///     The broker can also avoid this and return "success" to the client.
        ///     This feature allows using the MQTT Broker as the Frontend and another system as the backend.
        /// </summary>
        public bool ProcessSubscription { get; set; } = true;

        /// <summary>
        ///     Gets or sets the reason string which will be sent to the client in the SUBACK packet.
        /// </summary>
        public string ReasonString { get; set; }

        /// <summary>
        ///     Gets the response which will be sent to the client via the SUBACK packet.
        /// </summary>
        public SubscribeResponse Response { get; } = new SubscribeResponse();

        /// <summary>
        ///     Gets the current client session.
        /// </summary>
        public MqttSessionStatus Session { get; }

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems => Session.Items;

        /// <summary>
        ///     Gets or sets the topic filter.
        ///     The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; set; }

        /// <summary>
        ///     Gets or sets the user properties.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}