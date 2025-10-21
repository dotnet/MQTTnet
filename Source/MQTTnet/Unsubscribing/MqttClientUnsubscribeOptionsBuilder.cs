// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet;

public sealed class MqttClientUnsubscribeOptionsBuilder
{
    readonly MqttClientUnsubscribeOptions _unsubscribeOptions = new();

    public MqttClientUnsubscribeOptions Build()
    {
        return _unsubscribeOptions;
    }

    public MqttClientUnsubscribeOptionsBuilder WithTopicFilter(string topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        _unsubscribeOptions.TopicFilters ??= [];
        _unsubscribeOptions.TopicFilters.Add(topic);

        return this;
    }

    public MqttClientUnsubscribeOptionsBuilder WithTopicFilter(MqttTopicFilter topicFilter)
    {
        ArgumentNullException.ThrowIfNull(topicFilter);

        return WithTopicFilter(topicFilter.Topic);
    }

    /// <summary>
    ///     Adds the user property to the unsubscribe options.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientUnsubscribeOptionsBuilder WithUserProperty(string name, string value)
    {
        return WithUserProperty(new MqttUserProperty(name, value));
    }

    /// <summary>
    ///     Adds the user property to the unsubscribe options.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientUnsubscribeOptionsBuilder WithUserProperty(MqttUserProperty userProperty)
    {
        ArgumentNullException.ThrowIfNull(userProperty);

        _unsubscribeOptions.UserProperties ??= [];

        _unsubscribeOptions.UserProperties.Add(userProperty);

        return this;
    }
}