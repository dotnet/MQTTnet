// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public sealed class MqttClientSubscribeOptionsBuilder
{
    readonly MqttClientSubscribeOptions _subscribeOptions = new();

    public MqttClientSubscribeOptions Build()
    {
        return _subscribeOptions;
    }

    public MqttClientSubscribeOptionsBuilder WithSubscriptionIdentifier(uint subscriptionIdentifier)
    {
        if (subscriptionIdentifier == 0)
        {
            throw new MqttProtocolViolationException("Subscription identifier cannot be 0.");
        }

        _subscribeOptions.SubscriptionIdentifier = subscriptionIdentifier;
        return this;
    }

    public MqttClientSubscribeOptionsBuilder WithTopicFilter(
        string topic,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool noLocal = false,
        bool retainAsPublished = false,
        MqttRetainHandling retainHandling = MqttRetainHandling.SendAtSubscribe)
    {
        return WithTopicFilter(
            new MqttTopicFilter
            {
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                NoLocal = noLocal,
                RetainAsPublished = retainAsPublished,
                RetainHandling = retainHandling
            });
    }

    public MqttClientSubscribeOptionsBuilder WithTopicFilter(Action<MqttTopicFilterBuilder> topicFilterBuilder)
    {
        ArgumentNullException.ThrowIfNull(topicFilterBuilder);

        var internalTopicFilterBuilder = new MqttTopicFilterBuilder();
        topicFilterBuilder(internalTopicFilterBuilder);

        return WithTopicFilter(internalTopicFilterBuilder);
    }

    public MqttClientSubscribeOptionsBuilder WithTopicFilter(MqttTopicFilterBuilder topicFilterBuilder)
    {
        ArgumentNullException.ThrowIfNull(topicFilterBuilder);

        return WithTopicFilter(topicFilterBuilder.Build());
    }

    public MqttClientSubscribeOptionsBuilder WithTopicFilter(MqttTopicFilter topicFilter)
    {
        ArgumentNullException.ThrowIfNull(topicFilter);

        _subscribeOptions.TopicFilters ??= [];
        _subscribeOptions.TopicFilters.Add(topicFilter);

        return this;
    }

    /// <summary>
    ///     Adds the user property to the subscribe options.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    [Obsolete("Use the WithUserProperty accepting a ReadOnlyMemory<byte> for better performance.")]
    public MqttClientSubscribeOptionsBuilder WithUserProperty(string name, string value)
    {
        _subscribeOptions.UserProperties ??= [];
        _subscribeOptions.UserProperties.Add(new MqttUserProperty(name, value));

        return this;
    }

    /// <summary>
    ///     Adds the user property to the subscribe options.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientSubscribeOptionsBuilder WithUserProperty(string name, ReadOnlyMemory<byte> value)
    {
        _subscribeOptions.UserProperties ??= [];
        _subscribeOptions.UserProperties.Add(new MqttUserProperty(name, value));

        return this;
    }

    /// <summary>
    ///     Adds the user property to the subscribe options.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientSubscribeOptionsBuilder WithUserProperty(string name, ArraySegment<byte> value)
    {
        _subscribeOptions.UserProperties ??= [];
        _subscribeOptions.UserProperties.Add(new MqttUserProperty(name, value));

        return this;
    }
}