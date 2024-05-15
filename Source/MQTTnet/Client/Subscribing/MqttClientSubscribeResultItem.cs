// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Client;

public sealed class MqttClientSubscribeResultItem
{
    public MqttClientSubscribeResultItem(MqttTopicFilter topicFilter, MqttClientSubscribeResultCode resultCode)
    {
        TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        ResultCode = resultCode;
    }

    /// <summary>
    ///     Gets or sets the result code.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientSubscribeResultCode ResultCode { get; }

    /// <summary>
    ///     Gets or sets the topic filter.
    ///     The topic filter can contain topics and wildcards.
    /// </summary>
    public MqttTopicFilter TopicFilter { get; }
}