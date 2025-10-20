// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public sealed class MqttClientConnectResult
{
    /// <summary>
    ///     Gets the client identifier which was chosen by the server.
    ///     MQTTv5 only.
    /// </summary>
    public string? AssignedClientIdentifier { get; init; }

    /// <summary>
    ///     Gets the authentication data.
    ///     MQTTv5 only.
    /// </summary>
    public byte[]? AuthenticationData { get; init; }

    /// <summary>
    ///     Gets the authentication method.
    ///     MQTTv5 only.
    /// </summary>
    public string? AuthenticationMethod { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a session was already available or not.
    /// </summary>
    public bool IsSessionPresent { get; init; }

    public uint? MaximumPacketSize { get; init; }

    /// <summary>
    ///     Gets the maximum QoS which is supported by the server.
    ///     MQTTv5 only.
    /// </summary>
    public MqttQualityOfServiceLevel MaximumQoS { get; init; }

    /// <summary>
    ///     Gets the reason string.
    ///     MQTTv5 only.
    /// </summary>
    public string? ReasonString { get; init; }

    public ushort? ReceiveMaximum { get; init; }

    /// <summary>
    ///     Gets the response information.
    ///     MQTTv5 only.
    /// </summary>
    public string? ResponseInformation { get; init; }

    /// <summary>
    ///     Gets the result code.
    /// </summary>
    public MqttClientConnectResultCode ResultCode { get; init; }

    /// <summary>
    ///     Gets whether the server supports retained messages.
    ///     MQTTv5 only.
    /// </summary>
    public bool RetainAvailable { get; init; }

    /// <summary>
    ///     MQTTv5 only.
    ///     Gets the keep alive interval which was chosen by the server instead of the
    ///     keep alive interval from the client CONNECT packet.
    ///     A value of 0 indicates that the feature is not used.
    /// </summary>
    public ushort ServerKeepAlive { get; init; }

    /// <summary>
    ///     Gets an alternate server which should be used instead of the current one.
    ///     MQTTv5 only.
    /// </summary>
    public string? ServerReference { get; init; }

    public uint? SessionExpiryInterval { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the shared subscriptions are available or not.
    ///     MQTTv5 only.
    /// </summary>
    public bool SharedSubscriptionAvailable { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the subscription identifiers are available or not.
    ///     MQTTv5 only.
    /// </summary>
    public bool SubscriptionIdentifiersAvailable { get; init; }

    /// <summary>
    ///     Gets the maximum value for a topic alias. 0 means not supported.
    ///     MQTTv5 only.
    /// </summary>
    public ushort TopicAliasMaximum { get; init; }

    /// <summary>
    ///     Gets the user properties.
    ///     In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT
    ///     packet.
    ///     As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add
    ///     metadata to MQTT messages and pass information between publisher, broker, and subscriber.
    ///     The feature is very similar to the HTTP header concept.
    ///     MQTTv5 only.
    /// </summary>
    public List<MqttUserProperty>? UserProperties { get; init; }

    /// <summary>
    ///     Gets a value indicating whether wildcards can be used in subscriptions at the current server.
    ///     MQTTv5 only.
    /// </summary>
    public bool WildcardSubscriptionAvailable { get; init; }
}
