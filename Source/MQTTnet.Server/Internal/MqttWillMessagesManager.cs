// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MqttPublishPacketFactory = MQTTnet.Server.Internal.Formatter.MqttPublishPacketFactory;

namespace MQTTnet.Server.Internal;

internal sealed class MqttWillMessagesManager : IAsyncDisposable
{
    readonly MqttNetSourceLogger _logger;
    readonly MqttClientSessionsManager _sessionsManager;
    readonly Dictionary<MqttSession, PendingWill> _pending = new();
    readonly HashSet<PendingWill> _scheduled = new();
    readonly List<PendingWill> _ready = new();
    readonly object _syncRoot = new();
    readonly AsyncLocal<bool> _dispatching = new();
    CancellationTokenSource _cancellation;
    Task _worker = Task.CompletedTask;
    bool _acceptConnections;
    bool _flushOnStop;
    int _generation;

    public MqttWillMessagesManager(MqttClientSessionsManager sessionsManager, IMqttNetLogger logger)
    {
        _sessionsManager = sessionsManager;
        _logger = logger.WithSource(nameof(MqttWillMessagesManager));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(true).ConfigureAwait(false);
    }

    public void Connected(MqttConnectedClient client)
    {
        lock (_syncRoot)
        {
            if (!_acceptConnections)
            {
                return;
            }

            if (_pending.Remove(client.Session, out var previous))
            {
                _scheduled.Remove(previous);
                // Detach the old Will before replacing its connection metadata. An overdue
                // Will cannot be cancelled merely because the timer has not run yet.
                if (previous.Owner != null && !previous.Owner.IsRunning)
                {
                    Arm(previous);
                }

                if (!IsNormalDisconnect(previous.Owner) &&
                    (previous.Due <= DateTime.UtcNow || (previous.Owner != null && previous.Delay == 0)))
                {
                    previous.Due = DateTime.UtcNow;
                    _ready.Add(previous);
                }
            }

            client.Session.WillMessageSent = false;
            if (client.ConnectPacket.WillFlag)
            {
                _pending[client.Session] = new PendingWill
                {
                    Session = client.Session,
                    Owner = client,
                    SenderId = client.Id,
                    UserName = client.UserName,
                    Items = client.Session.Items,
                    Message = MqttApplicationMessageFactory.Create(MqttPublishPacketFactory.Create(client.ConnectPacket)),
                    Delay = client.ChannelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500 ? client.ConnectPacket.WillDelayInterval : 0
                };
            }
        }
    }

    public void Disconnected(MqttConnectedClient client)
    {
        lock (_syncRoot)
        {
            if (!_pending.TryGetValue(client.Session, out var pending) || pending.Owner != client)
            {
                return;
            }

            if (IsNormalDisconnect(client))
            {
                _pending.Remove(client.Session);
                return;
            }

            Arm(pending);
            pending.Owner = null;
            _scheduled.Add(pending);
        }
    }

    public void SessionEnded(MqttSession session)
    {
        lock (_syncRoot)
        {
            if (_pending.Remove(session, out var pending) && !IsNormalDisconnect(pending.Owner))
            {
                _scheduled.Remove(pending);
                pending.Due = DateTime.UtcNow;
                _ready.Add(pending);
            }
        }
    }

    public async Task StartAsync()
    {
        CancellationToken token;
        int generation;
        lock (_syncRoot)
        {
            _acceptConnections = true;
            _flushOnStop = false;
            _cancellation = new CancellationTokenSource();
            token = _cancellation.Token;
            generation = ++_generation;
        }

        // Deliver overdue Wills before the adapters begin accepting resumed sessions.
        await DispatchReadyAsync(generation).ConfigureAwait(false);
        lock (_syncRoot)
        {
            if (_acceptConnections && _generation == generation)
            {
                _worker = Task.Run(() => RunAsync(generation, token), token);
            }
        }
    }

    public async Task StopAsync(bool endSessions)
    {
        CancellationTokenSource cancellation;
        Task worker;
        int generation;
        lock (_syncRoot)
        {
            _acceptConnections = false;
            _flushOnStop |= endSessions;
            endSessions = _flushOnStop;
            generation = ++_generation;
            // StopAsync on a connection does not wait for its receive-loop cleanup. Capture
            // every transition now; a late Disconnected call cannot arm the Will twice.
            foreach (var pair in _pending.ToList())
            {
                var pending = pair.Value;
                if (IsNormalDisconnect(pending.Owner))
                {
                    _pending.Remove(pair.Key);
                    _scheduled.Remove(pending);
                    continue;
                }

                if (pending.Owner != null)
                {
                    Arm(pending);
                    pending.Owner = null;
                }

                if (endSessions)
                {
                    pending.Due = DateTime.UtcNow;
                }

                _scheduled.Add(pending);
            }

            cancellation = _cancellation;
            worker = _worker;
            cancellation?.Cancel();
        }

        try
        {
            // A publication callback may stop the server. It must not await itself.
            if (!_dispatching.Value)
            {
                await worker.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }

        lock (_syncRoot)
        {
            if (_cancellation == cancellation)
            {
                _cancellation = null;
            }
        }

        cancellation?.Dispose();
        if (endSessions && !_dispatching.Value)
        {
            await DispatchReadyAsync(generation, true).ConfigureAwait(false);
        }
    }

    static bool IsNormalDisconnect(MqttConnectedClient client)
    {
        return client?.DisconnectPacket?.ReasonCode == MqttDisconnectReasonCode.NormalDisconnection;
    }

    static void Arm(PendingWill pending)
    {
        // Session expiry is the effective interval owned by the session lifecycle.
        // DateTime supports the entire uint interval range without Task.Delay overflow.
        pending.Due = DateTime.UtcNow.AddSeconds(Math.Min(pending.Delay, pending.Session.ExpiryInterval));
    }

    async Task DispatchReadyAsync(int generation, bool flush = false)
    {
        lock (_syncRoot)
        {
            MoveDueWills();
        }

        while (true)
        {
            PendingWill pending;
            lock (_syncRoot)
            {
                if (_generation != generation || (!flush && !_acceptConnections))
                {
                    if (!_acceptConnections && _flushOnStop)
                    {
                        // A callback ended the sessions. Let this dispatch loop finish the
                        // flush after the callback returns instead of recursively invoking it.
                        generation = _generation;
                        flush = true;
                        MoveDueWills();
                    }
                    else
                    {
                        return;
                    }
                }

                // Claim one message at a time. If a callback stops the server, the rest
                // remain owned by the scheduler for restart or final session disposal.
                pending = _ready.FirstOrDefault(p => p.Owner == null || !p.Owner.IsRunning);
                if (pending == null)
                {
                    return;
                }

                _ready.Remove(pending);
            }

            var wasDispatching = _dispatching.Value;
            _dispatching.Value = true;
            try
            {
                // Message Expiry starts here, not when the delayed Will was registered.
                await _sessionsManager.DispatchApplicationMessage(pending.SenderId, pending.UserName, pending.Items, pending.Message, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error publishing Will for client '{0}'", pending.SenderId);
            }
            finally
            {
                _dispatching.Value = wasDispatching;
            }
        }
    }

    async Task RunAsync(int generation, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await DispatchReadyAsync(generation).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    void MoveDueWills()
    {
        var now = DateTime.UtcNow;
        foreach (var pending in _scheduled.ToList())
        {
            if (pending.Due <= now)
            {
                _scheduled.Remove(pending);
                _pending.Remove(pending.Session);
                pending.Session.WillMessageSent = true;
                _ready.Add(pending);
            }
        }
    }

    sealed class PendingWill
    {
        public MqttSession Session;
        public MqttConnectedClient Owner;
        public string SenderId;
        public string UserName;
        public IDictionary Items;
        public MqttApplicationMessage Message;
        public uint Delay;
        public DateTime? Due;
    }
}