// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttClientUnsubscribeResultItem
    {
        /// <summary>
        ///     Gets or sets the result code.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientUnsubscribeResultCode ResultCode { get; internal set; }

        /// <summary>
        ///     Gets or sets the topic filter.
        ///     The topic filter can contain topics and wildcards.
        /// </summary>
        public string TopicFilter { get; internal set; }
    }
}