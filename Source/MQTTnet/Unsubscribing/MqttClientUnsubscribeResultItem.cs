// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet;

public sealed class MqttClientUnsubscribeResultItem
{
    public MqttClientUnsubscribeResultItem(string topicFilter, MqttClientUnsubscribeResultCode resultCode)
    {
        TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        ResultCode = resultCode;
    }

    /// <summary>
    ///     Gets or sets the result code.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientUnsubscribeResultCode ResultCode { get; }

    /// <summary>
    ///     Gets or sets the topic filter.
    ///     The topic filter can contain topics and wildcards.
    /// </summary>
    public string TopicFilter { get; }
}