// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     Background worker that tracks pending <see cref="DelayedMessage"/>s ordered by due time
///     and dispatches them through the provided <see cref="MqttServer"/> when due.
/// </summary>
sealed class DelayedMessageScheduler : IAsyncDisposable
{
    readonly MqttServer _server;
    readonly IDelayedMessageStore _store;
    readonly DelayedPublishOptions _options;
    readonly MqttNetSourceLogger _logger;

    readonly object _sync = new();
    readonly PriorityQueue<Guid, long> _queue = new();
    readonly Dictionary<Guid, DelayedMessage> _pending = new();

    // Pulses the worker whenever the head of the queue may have changed.
    readonly SemaphoreSlim _signal = new(0, int.MaxValue);
    readonly CancellationTokenSource _cts = new();
    Task _workerTask;

    public DelayedMessageScheduler(
        MqttServer server,
        IDelayedMessageStore store,
        DelayedPublishOptions options,
        IMqttNetLogger logger)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = (logger ?? MqttNetNullLogger.Instance).WithSource(nameof(DelayedMessageScheduler));
    }

    public int PendingCount
    {
        get
        {
            lock (_sync)
            {
                return _pending.Count;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var existing = await _store.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            lock (_sync)
            {
                foreach (var message in existing)
                {
                    if (_pending.ContainsKey(message.Id))
                    {
                        continue;
                    }

                    _pending[message.Id] = message;
                    _queue.Enqueue(message.Id, message.DueUtc.UtcTicks);
                }
            }
        }

        _workerTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);

        // Ensure the worker re-evaluates the head after rehydration.
        Pulse();
    }

    /// <summary>
    ///     Tries to enqueue a new delayed message. Returns <c>false</c> when the pending cap
    ///     (<see cref="DelayedPublishOptions.MaxPendingMessages"/>) has been reached.
    /// </summary>
    public async ValueTask<bool> TryEnqueueAsync(DelayedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_sync)
        {
            if (_pending.Count >= _options.MaxPendingMessages)
            {
                return false;
            }

            _pending[message.Id] = message;
            _queue.Enqueue(message.Id, message.DueUtc.UtcTicks);
        }

        try
        {
            await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_sync)
            {
                _pending.Remove(message.Id);
                // The queue entry will be skipped when it surfaces (see RunAsync).
            }

            throw;
        }

        Pulse();
        return true;
    }

    void Pulse()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already saturated; worker will wake up anyway.
        }
    }

    async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            DelayedMessage dueMessage = null;
            TimeSpan waitTime;

            lock (_sync)
            {
                while (_queue.TryPeek(out var id, out var dueTicks))
                {
                    if (!_pending.TryGetValue(id, out var candidate))
                    {
                        // Stale entry (removed concurrently). Drop it.
                        _queue.Dequeue();
                        continue;
                    }

                    var now = DateTimeOffset.UtcNow.UtcTicks;
                    if (dueTicks <= now)
                    {
                        _queue.Dequeue();
                        _pending.Remove(id);
                        dueMessage = candidate;
                    }

                    break;
                }

                if (dueMessage == null)
                {
                    if (_queue.TryPeek(out _, out var nextDueTicks))
                    {
                        var delta = nextDueTicks - DateTimeOffset.UtcNow.UtcTicks;
                        waitTime = delta > 0 ? TimeSpan.FromTicks(delta) : TimeSpan.Zero;
                    }
                    else
                    {
                        waitTime = Timeout.InfiniteTimeSpan;
                    }
                }
                else
                {
                    waitTime = TimeSpan.Zero;
                }
            }

            if (dueMessage != null)
            {
                await DispatchAsync(dueMessage, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await _signal.WaitAsync(waitTime, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    async Task DispatchAsync(DelayedMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var payload = message.Message;

            // MQTT 5: MessageExpiryInterval counts from broker receipt. Decrement by the elapsed
            // delay; drop messages that have already expired.
            if (payload.MessageExpiryInterval > 0)
            {
                var elapsedSeconds = (uint)Math.Min(
                    uint.MaxValue,
                    Math.Max(0, (long)(DateTimeOffset.UtcNow - message.ReceivedUtc).TotalSeconds));

                if (elapsedSeconds >= payload.MessageExpiryInterval)
                {
                    _logger.Verbose("Dropping delayed message {0} because MessageExpiryInterval elapsed.", message.Id);
                    await _store.RemoveAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    return;
                }

                payload = CloneWithExpiry(payload, payload.MessageExpiryInterval - elapsedSeconds);
            }

            var injected = new InjectedMqttApplicationMessage(payload);

            if (_options.PreserveSenderIdentity)
            {
                injected.SenderClientId = message.SenderClientId;
                injected.SenderUserName = message.SenderUserName;
            }

            await _server.InjectApplicationMessage(injected, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to dispatch delayed message {0}.", message.Id);
        }
        finally
        {
            try
            {
                await _store.RemoveAsync(message.Id, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to remove delayed message {0} from store.", message.Id);
            }
        }
    }

    static MqttApplicationMessage CloneWithExpiry(MqttApplicationMessage source, uint newExpiry)
    {
        return new MqttApplicationMessage
        {
            Topic = source.Topic,
            Payload = source.Payload,
            QualityOfServiceLevel = source.QualityOfServiceLevel,
            Retain = source.Retain,
            ContentType = source.ContentType,
            CorrelationData = source.CorrelationData,
            Dup = source.Dup,
            MessageExpiryInterval = newExpiry,
            PayloadFormatIndicator = source.PayloadFormatIndicator,
            ResponseTopic = source.ResponseTopic,
            SubscriptionIdentifiers = source.SubscriptionIdentifiers,
            TopicAlias = source.TopicAlias,
            UserProperties = source.UserProperties
        };
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_workerTask != null)
        {
            try
            {
                await _workerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        _cts.Dispose();
        _signal.Dispose();
    }
}
