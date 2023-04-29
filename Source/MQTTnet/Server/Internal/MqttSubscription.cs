// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttSubscription
    {
        public MqttSubscription(
            string topic,
            bool noLocal,
            MqttRetainHandling retainHandling,
            bool retainAsPublished,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            uint identifier)
        {
            Topic = topic;
            NoLocal = noLocal;
            RetainHandling = retainHandling;
            RetainAsPublished = retainAsPublished;
            GrantedQualityOfServiceLevel = qualityOfServiceLevel;
            Identifier = identifier;

            MqttTopicHash.Calculate(Topic, out var hash, out var hashMask, out var hasWildcard);
            TopicHash = hash;
            TopicHashMask = hashMask;
            TopicHasWildcard = hasWildcard;
        }

        public MqttQualityOfServiceLevel GrantedQualityOfServiceLevel { get; }

        public uint Identifier { get; }

        public bool NoLocal { get; }

        public bool RetainAsPublished { get; }

        public MqttRetainHandling RetainHandling { get; }

        public string Topic { get; }

        public ulong TopicHash { get; }

        public ulong TopicHashMask { get; }

        public bool TopicHasWildcard { get; }
    }
}