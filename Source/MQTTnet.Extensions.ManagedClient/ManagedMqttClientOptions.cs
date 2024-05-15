// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient;

public sealed class ManagedMqttClientOptions
{
    public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
    public MqttClientOptions ClientOptions { get; set; }

    public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

    public int MaxPendingMessages { get; set; } = int.MaxValue;

    /// <summary>
    ///     Defines the maximum amount of topic filters which will be sent in a SUBSCRIBE/UNSUBSCRIBE packet.
    ///     Amazon Web Services (AWS) limits this number to 8. The default is int.MaxValue.
    /// </summary>
    public int MaxTopicFiltersInSubscribeUnsubscribePackets { get; set; } = int.MaxValue;

    public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropNewMessage;

    public IManagedMqttClientStorage Storage { get; set; }
}