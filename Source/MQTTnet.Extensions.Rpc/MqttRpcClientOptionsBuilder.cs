// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.Rpc;

public sealed class MqttRpcClientOptionsBuilder
{
    IMqttRpcClientTopicGenerationStrategy _topicGenerationStrategy = new DefaultMqttRpcClientTopicGenerationStrategy();

    public MqttRpcClientOptions Build()
    {
        return new MqttRpcClientOptions
        {
            TopicGenerationStrategy = _topicGenerationStrategy
        };
    }

    public MqttRpcClientOptionsBuilder WithTopicGenerationStrategy(IMqttRpcClientTopicGenerationStrategy value)
    {
        _topicGenerationStrategy = value ?? throw new ArgumentNullException(nameof(value));

        return this;
    }
}