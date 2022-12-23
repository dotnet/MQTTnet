// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Exceptions;

namespace MQTTnet.Protocol
{
    public static class MqttTopicValidator
    {
        public static void ThrowIfInvalid(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.TopicAlias == 0)
            {
                ThrowIfInvalid(applicationMessage.Topic);
            }
        }

        public static void ThrowIfInvalid(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new MqttProtocolViolationException("Topic should not be empty.");
            }

            foreach (var @char in topic)
            {
                if (@char == '+')
                {
                    throw new MqttProtocolViolationException("The character '+' is not allowed in topics.");
                }

                if (@char == '#')
                {
                    throw new MqttProtocolViolationException("The character '#' is not allowed in topics.");
                }
            }
        }

        public static void ThrowIfInvalidSubscribe(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new MqttProtocolViolationException("Topic should not be empty.");
            }

            var indexOfHash = topic.IndexOf("#", StringComparison.Ordinal);
            if (indexOfHash != -1 && indexOfHash != topic.Length - 1)
            {
                throw new MqttProtocolViolationException("The character '#' is only allowed as last character");
            }
        }
    }
}