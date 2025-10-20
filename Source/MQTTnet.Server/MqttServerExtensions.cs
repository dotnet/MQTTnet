// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server;

public static class MqttServerExtensions
{
    public static Task DisconnectClientAsync(this MqttServer server, string id, MqttDisconnectReasonCode reasonCode = MqttDisconnectReasonCode.NormalDisconnection)
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.DisconnectClientAsync(id, new MqttServerClientDisconnectOptions { ReasonCode = reasonCode });
    }

    public static Task InjectApplicationMessage(
        this MqttServer server,
        string topic,
        string? payload = null,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(topic);

        var payloadBuffer = EmptyBuffer.Array;
        if (payload != null)
        {
            payloadBuffer = Encoding.UTF8.GetBytes(payload);
        }

        return server.InjectApplicationMessage(
            new InjectedMqttApplicationMessage(
                new MqttApplicationMessage
                {
                    Topic = topic,
                    PayloadSegment = new ArraySegment<byte>(payloadBuffer),
                    QualityOfServiceLevel = qualityOfServiceLevel,
                    Retain = retain
                }));
    }

    public static Task StopAsync(this MqttServer server)
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.StopAsync(new MqttServerStopOptions());
    }

    public static Task SubscribeAsync(this MqttServer server, string clientId, params MqttTopicFilter[] topicFilters)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(topicFilters);

        return server.SubscribeAsync(clientId, topicFilters);
    }

    public static Task SubscribeAsync(this MqttServer server, string clientId, string topic)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(topic);

        var topicFilters = new MqttTopicFilterBuilder().WithTopic(topic).Build();
        return server.SubscribeAsync(clientId, topicFilters);
    }
}