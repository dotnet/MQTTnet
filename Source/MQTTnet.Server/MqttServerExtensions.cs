// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Server;

public static class MqttServerExtensions
{
    public static Task DisconnectClientAsync(this MqttServer server, string id, MqttDisconnectReasonCode reasonCode = MqttDisconnectReasonCode.NormalDisconnection)
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.DisconnectClientAsync(id, new MqttServerClientDisconnectOptions { ReasonCode = reasonCode });
    }

    [Obsolete("Use method InjectStringAsync() instead.")]
    public static Task InjectApplicationMessage(
        this MqttServer server,
        string topic,
        string payload = null,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false)
    {
        return server.InjectStringAsync(string.Empty, topic, payload, qualityOfServiceLevel, retain);
    }

    public static Task InjectApplicationMessage(
        this MqttServer server,
        string clientId,
        MqttApplicationMessage applicationMessage,
        IDictionary customSessionItems = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var injectedApplicationMessage = new InjectedMqttApplicationMessage(applicationMessage)
        {
            SenderClientId = clientId,
            CustomSessionItems = customSessionItems,
        };
        return server.InjectApplicationMessage(injectedApplicationMessage, cancellationToken);
    }

    public static Task InjectSequenceAsync(
        this MqttServer server,
        string clientId,
        string topic,
        ReadOnlySequence<byte> payload,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(topic);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build();

        return server.InjectApplicationMessage(clientId, applicationMessage, customSessionItems: null, cancellationToken);
    }

    public static async Task InjectSequenceAsync(
        this MqttServer server,
        string clientId,
        string topic,
        Func<PipeWriter, ValueTask> payloadFactory,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        await using var payloadOwner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(payloadFactory, cancellationToken);
        await server.InjectSequenceAsync(clientId, topic, payloadOwner.Payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public static Task InjectBinaryAsync(
       this MqttServer server,
       string clientId,
       string topic,
       ReadOnlyMemory<byte> payload,
       MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
       bool retain = false,
       CancellationToken cancellationToken = default)
    {
        return server.InjectSequenceAsync(clientId, topic, new ReadOnlySequence<byte>(payload), qualityOfServiceLevel, retain, cancellationToken);
    }

    public static async Task InjectBinaryAsync(
       this MqttServer server,
       string clientId,
       string topic,
       int payloadSize,
       Action<Memory<byte>> payloadFactory,
       MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
       bool retain = false,
       CancellationToken cancellationToken = default)
    {
        await using var payloadOwner = MqttPayloadOwnerFactory.CreateSingleSegment(payloadSize, out var payloadMemory);
        payloadFactory?.Invoke(payloadMemory);
        await server.InjectSequenceAsync(clientId, topic, payloadOwner.Payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public static async Task InjectStringAsync(
       this MqttServer server,
       string clientId,
       string topic,
       string payload,
       MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
       bool retain = false,
       CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(payload))
        {
            await server.InjectSequenceAsync(clientId, topic, ReadOnlySequence<byte>.Empty, qualityOfServiceLevel, retain, cancellationToken);
        }
        else
        {
            await server.InjectSequenceAsync(clientId, topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);
        }

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            Encoding.UTF8.GetBytes(payload, writer);
            await writer.FlushAsync(cancellationToken);
        }
    }

    public static async Task InjectJsonAsync<TValue>(
        this MqttServer server,
        string clientId,
        string topic,
        TValue payload,
        JsonSerializerOptions jsonSerializerOptions = default,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        await server.InjectSequenceAsync(clientId, topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            var stream = writer.AsStream(leaveOpen: true);
            await JsonSerializer.SerializeAsync(stream, payload, jsonSerializerOptions, cancellationToken);
        }
    }


    public static async Task InjectJsonAsync<TValue>(
        this MqttServer server,
        string clientId,
        string topic,
        TValue payload,
        JsonTypeInfo<TValue> jsonTypeInfo,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        await server.InjectSequenceAsync(clientId, topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            var stream = writer.AsStream(leaveOpen: true);
            await JsonSerializer.SerializeAsync(stream, payload, jsonTypeInfo, cancellationToken);
        }
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