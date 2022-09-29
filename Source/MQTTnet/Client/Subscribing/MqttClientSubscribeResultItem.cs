// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientSubscribeResultItem
    {
        /// <summary>
        ///     Gets or sets the result code.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientSubscribeResultCode ResultCode { get; internal set; }

        /// <summary>
        ///     Gets or sets the topic filter.
        ///     The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; internal set; }
    }
}