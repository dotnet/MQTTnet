// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet;

public static class MqttClientExtensions
{
    public static Task DisconnectAsync(
        this IMqttClient client,
        MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
        string reasonString = null,
        uint sessionExpiryInterval = 0,
        List<MqttUserProperty> userProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var disconnectOptions = new MqttClientDisconnectOptions
        {
            Reason = reason,
            ReasonString = reasonString,
            SessionExpiryInterval = sessionExpiryInterval,
            UserProperties = userProperties
        };

        return client.DisconnectAsync(disconnectOptions, cancellationToken);
    }

    public static Task<MqttClientPublishResult> PublishSequenceAsync(
        this IMqttClient mqttClient,
        string topic,
        ReadOnlySequence<byte> payload,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mqttClient);
        ArgumentNullException.ThrowIfNull(topic);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build();

        return mqttClient.PublishAsync(applicationMessage, cancellationToken);
    }

    public static async Task<MqttClientPublishResult> PublishSequenceAsync(
        this IMqttClient mqttClient,
        string topic,
        Func<PipeWriter, ValueTask> payloadFactory,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        await using var payloadOwner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(payloadFactory, cancellationToken);
        return await mqttClient.PublishSequenceAsync(topic, payloadOwner.Payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public static Task<MqttClientPublishResult> PublishBinaryAsync(
        this IMqttClient mqttClient,
        string topic,
        ReadOnlyMemory<byte> payload = default,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        return mqttClient.PublishSequenceAsync(topic, new ReadOnlySequence<byte>(payload), qualityOfServiceLevel, retain, cancellationToken);
    }

    public static async Task<MqttClientPublishResult> PublishBinaryAsync(
       this IMqttClient mqttClient,
       string topic,
       int payloadSize,
       Action<Memory<byte>> payloadFactory,
       MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
       bool retain = false,
       CancellationToken cancellationToken = default)
    {
        await using var payloadOwner = MqttPayloadOwnerFactory.CreateSingleSegment(payloadSize, out var payloadMemory);
        payloadFactory?.Invoke(payloadMemory);
        return await mqttClient.PublishSequenceAsync(topic, payloadOwner.Payload, qualityOfServiceLevel, retain, cancellationToken);
    }

    public static async Task<MqttClientPublishResult> PublishStringAsync(
        this IMqttClient mqttClient,
        string topic,
        string payload = default,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrEmpty(payload)
            ? await mqttClient.PublishSequenceAsync(topic, ReadOnlySequence<byte>.Empty, qualityOfServiceLevel, retain, cancellationToken)
            : await mqttClient.PublishSequenceAsync(topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            Encoding.UTF8.GetBytes(payload, writer);
            await writer.FlushAsync(cancellationToken);
        }
    }

    public static async Task<MqttClientPublishResult> PublishJsonAsync<TValue>(
        this IMqttClient mqttClient,
        string topic,
        TValue payload,
        JsonSerializerOptions jsonSerializerOptions = default,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        return await mqttClient.PublishSequenceAsync(topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            var stream = writer.AsStream(leaveOpen: true);
            await JsonSerializer.SerializeAsync(stream, payload, jsonSerializerOptions, cancellationToken);
        }
    }

    public static async Task<MqttClientPublishResult> PublishJsonAsync<TValue>(
        this IMqttClient mqttClient,
        string topic,
        TValue payload,
        JsonTypeInfo<TValue> jsonTypeInfo,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        return await mqttClient.PublishSequenceAsync(topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            var stream = writer.AsStream(leaveOpen: true);
            await JsonSerializer.SerializeAsync(stream, payload, jsonTypeInfo, cancellationToken);
        }
    }

    public static async Task<MqttClientPublishResult> PublishStreamAsync(
        this IMqttClient mqttClient,
        string topic,
        Stream payload,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return await mqttClient.PublishSequenceAsync(topic, WritePayloadAsync, qualityOfServiceLevel, retain, cancellationToken);

        async ValueTask WritePayloadAsync(PipeWriter writer)
        {
            await payload.CopyToAsync(writer, cancellationToken);
        }
    }


    public static Task ReconnectAsync(this IMqttClient client, CancellationToken cancellationToken = default)
    {
        if (client.Options == null)
        {
            throw new InvalidOperationException("The MQTT client was not connected before. A reconnect is only permitted when the client was already connected or at least tried to.");
        }

        return client.ConnectAsync(client.Options, cancellationToken);
    }

    public static Task SendExtendedAuthenticationExchangeDataAsync(this IMqttClient client, MqttExtendedAuthenticationExchangeData data)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SendExtendedAuthenticationExchangeDataAsync(data, CancellationToken.None);
    }

    public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient mqttClient, MqttTopicFilter topicFilter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mqttClient);
        ArgumentNullException.ThrowIfNull(topicFilter);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter).Build();

        return mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    public static Task<MqttClientSubscribeResult> SubscribeAsync(
        this IMqttClient mqttClient,
        string topic,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mqttClient);
        ArgumentNullException.ThrowIfNull(topic);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, qualityOfServiceLevel).Build();

        return mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    public static async Task<bool> TryDisconnectAsync(
        this IMqttClient client,
        MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
        string reasonString = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        try
        {
            await client.DisconnectAsync(reason, reasonString).ConfigureAwait(false);
            return true;
        }
        catch
        {
            // Ignore all errors.
        }

        return false;
    }

    public static async Task<bool> TryPingAsync(this IMqttClient client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        try
        {
            await client.PingAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            // Ignore errors.
        }

        return false;
    }

    public static Task<MqttClientUnsubscribeResult> UnsubscribeAsync(this IMqttClient mqttClient, string topic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mqttClient);
        ArgumentNullException.ThrowIfNull(topic);

        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic).Build();

        return mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken);
    }
}