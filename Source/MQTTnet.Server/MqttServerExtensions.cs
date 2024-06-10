// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server;

public static class MqttServerExtensions
{
    public static Task DisconnectClientAsync(this MqttServer server, string id, MqttDisconnectReasonCode reasonCode = MqttDisconnectReasonCode.NormalDisconnection)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        return server.DisconnectClientAsync(id, new MqttServerClientDisconnectOptions { ReasonCode = reasonCode });
    }

    public static Task InjectApplicationMessage(
        this MqttServer server,
        string topic,
        string payload = null,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (topic == null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var payloadBuffer = EmptyBuffer.Array;
        if (payload is string stringPayload)
        {
            payloadBuffer = Encoding.UTF8.GetBytes(stringPayload);
        }

        return server.InjectApplicationMessage(
            new InjectedMqttApplicationMessage(
                new MqttApplicationMessage
                {
                    Topic = topic,
                    Payload = new ReadOnlySequence<byte>(payloadBuffer),
                    QualityOfServiceLevel = qualityOfServiceLevel,
                    Retain = retain
                }));
    }

    public static Task StopAsync(this MqttServer server)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        return server.StopAsync(new MqttServerStopOptions());
    }

    public static Task SubscribeAsync(this MqttServer server, string clientId, params MqttTopicFilter[] topicFilters)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (clientId == null)
        {
            throw new ArgumentNullException(nameof(clientId));
        }

        if (topicFilters == null)
        {
            throw new ArgumentNullException(nameof(topicFilters));
        }

        return server.SubscribeAsync(clientId, topicFilters);
    }

    public static Task SubscribeAsync(this MqttServer server, string clientId, string topic)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (clientId == null)
        {
            throw new ArgumentNullException(nameof(clientId));
        }

        if (topic == null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var topicFilters = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        return server.SubscribeAsync(clientId, topicFilters);
    }
}