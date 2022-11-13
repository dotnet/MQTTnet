// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientPublishResult
    {
        /// <summary>
        ///     Returns if the overall status of the publish is a success. This can be the reason code _Success_ or
        ///     _NoMatchingSubscribers_. _NoMatchingSubscribers_ usually indicates only that no other client is interested in the topic but overall transmit
        ///     to the server etc. was a success.
        /// </summary>
        public bool IsSuccess => ReasonCode == MqttClientPublishReasonCode.Success || ReasonCode == MqttClientPublishReasonCode.NoMatchingSubscribers;

        /// <summary>
        ///     Gets the packet identifier which was used for this publish.
        /// </summary>
        public ushort? PacketIdentifier { get; set; }

        /// <summary>
        ///     Gets or sets the reason code.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientPublishReasonCode ReasonCode { get; set; } = MqttClientPublishReasonCode.Success;

        /// <summary>
        ///     Gets or sets the reason string.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; set; }

        /// <summary>
        ///     Gets or sets the user properties.
        ///     In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT
        ///     packet.
        ///     As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add
        ///     metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        ///     The feature is very similar to the HTTP header concept.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public IReadOnlyCollection<MqttUserProperty> UserProperties { get; internal set; }
    }
}