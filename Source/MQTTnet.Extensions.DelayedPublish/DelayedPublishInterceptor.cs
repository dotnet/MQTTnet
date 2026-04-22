// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     Handles <see cref="MqttServer.InterceptingPublishAsync"/>, detects the
///     <c>$delayed/&lt;interval&gt;/&lt;topic&gt;</c> convention and schedules the message for
///     later dispatch.
/// </summary>
sealed class DelayedPublishInterceptor
{
    readonly DelayedPublishOptions _options;
    readonly DelayedMessageScheduler _scheduler;
    readonly MqttNetSourceLogger _logger;
    readonly string _prefixWithSlash;

    public DelayedPublishInterceptor(
        DelayedPublishOptions options,
        DelayedMessageScheduler scheduler,
        IMqttNetLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _logger = (logger ?? MqttNetNullLogger.Instance).WithSource(nameof(DelayedPublishInterceptor));

        if (string.IsNullOrWhiteSpace(_options.TopicPrefix))
        {
            throw new ArgumentException("TopicPrefix must not be empty.", nameof(options));
        }

        _prefixWithSlash = _options.TopicPrefix + "/";
    }

    public async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);

        var topic = eventArgs.ApplicationMessage?.Topic;
        if (string.IsNullOrEmpty(topic) || !topic.StartsWith(_prefixWithSlash, StringComparison.Ordinal))
        {
            return;
        }

        if (!TryParse(topic, out var dueUtc, out var realTopic, out var rejectReason))
        {
            _logger.Warning("Rejecting delayed publish on topic '{0}': {1}", topic, rejectReason);
            eventArgs.ProcessPublish = false;
            eventArgs.Response.ReasonCode = MqttPubAckReasonCode.TopicNameInvalid;
            eventArgs.Response.ReasonString = rejectReason;
            return;
        }

        var rewritten = Rewrite(eventArgs.ApplicationMessage, realTopic);

        var delayedMessage = new DelayedMessage(
            Guid.NewGuid(),
            dueUtc,
            DateTimeOffset.UtcNow,
            eventArgs.ClientId,
            eventArgs.UserName,
            rewritten);

        if (_options.AuthorizeAsync != null)
        {
            bool authorized;
            try
            {
                authorized = await _options.AuthorizeAsync(delayedMessage, eventArgs.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Authorization callback for delayed publish threw.");
                eventArgs.ProcessPublish = false;
                eventArgs.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
                return;
            }

            if (!authorized)
            {
                eventArgs.ProcessPublish = false;
                eventArgs.Response.ReasonCode = MqttPubAckReasonCode.NotAuthorized;
                return;
            }
        }

        bool enqueued;
        try
        {
            enqueued = await _scheduler.TryEnqueueAsync(delayedMessage, eventArgs.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to enqueue delayed message from '{0}'.", eventArgs.ClientId);
            eventArgs.ProcessPublish = false;
            eventArgs.Response.ReasonCode = MqttPubAckReasonCode.UnspecifiedError;
            return;
        }

        if (!enqueued)
        {
            eventArgs.ProcessPublish = false;
            eventArgs.Response.ReasonCode = MqttPubAckReasonCode.QuotaExceeded;
            eventArgs.Response.ReasonString = "Delayed publish queue is full.";
            return;
        }

        // Accept the publish to trigger PUBACK/PUBCOMP for the publisher, but do not dispatch now.
        eventArgs.ProcessPublish = false;
        eventArgs.Response.ReasonCode = MqttPubAckReasonCode.Success;
    }

    bool TryParse(string topic, out DateTimeOffset dueUtc, out string realTopic, out string reason)
    {
        dueUtc = default;
        realTopic = null;
        reason = null;

        // topic = "<prefix>/<interval>/<realTopic...>"
        var afterPrefix = topic.AsSpan(_prefixWithSlash.Length);
        var separator = afterPrefix.IndexOf('/');
        if (separator <= 0 || separator == afterPrefix.Length - 1)
        {
            reason = "Missing interval or real topic segment.";
            return false;
        }

        var intervalSpan = afterPrefix[..separator];
        if (!long.TryParse(intervalSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var intervalValue) || intervalValue <= 0)
        {
            reason = "Interval segment must be a positive integer.";
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        DateTimeOffset computedDue;
        if (intervalValue <= _options.AbsoluteTimeThresholdSeconds)
        {
            computedDue = now.AddSeconds(intervalValue);
        }
        else
        {
            try
            {
                computedDue = DateTimeOffset.FromUnixTimeSeconds(intervalValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                reason = "Interval segment is out of range.";
                return false;
            }

            if (computedDue <= now)
            {
                reason = "Absolute due time is in the past.";
                return false;
            }
        }

        if (computedDue - now > _options.MaxDelay)
        {
            reason = "Requested delay exceeds MaxDelay.";
            return false;
        }

        realTopic = afterPrefix[(separator + 1)..].ToString();
        if (string.IsNullOrEmpty(realTopic))
        {
            reason = "Real topic segment is empty.";
            return false;
        }

        dueUtc = computedDue;
        return true;
    }

    static MqttApplicationMessage Rewrite(MqttApplicationMessage source, string realTopic)
    {
        return new MqttApplicationMessage
        {
            Topic = realTopic,
            Payload = source.Payload,
            QualityOfServiceLevel = source.QualityOfServiceLevel,
            Retain = source.Retain,
            ContentType = source.ContentType,
            CorrelationData = source.CorrelationData,
            Dup = source.Dup,
            MessageExpiryInterval = source.MessageExpiryInterval,
            PayloadFormatIndicator = source.PayloadFormatIndicator,
            ResponseTopic = source.ResponseTopic,
            SubscriptionIdentifiers = source.SubscriptionIdentifiers,
            TopicAlias = source.TopicAlias,
            UserProperties = source.UserProperties
        };
    }
}
