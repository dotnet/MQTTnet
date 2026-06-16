// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.Extensions.DelayedPublish;

public static class DelayedPublishServerExtensions
{
    /// <summary>
    ///     Enables EMQX-compatible delayed publish support on this <see cref="MqttServer"/> instance.
    ///     Messages published to <c>&lt;TopicPrefix&gt;/&lt;Interval&gt;/&lt;RealTopic&gt;</c> are held
    ///     by the broker and dispatched when the requested time elapses.
    /// </summary>
    /// <param name="server">The running <see cref="MqttServer"/>. Must have already been started.</param>
    /// <param name="options">Optional configuration. Defaults are used when <c>null</c>.</param>
    /// <param name="store">Optional persistent store. Defaults to <see cref="InMemoryDelayedMessageStore"/>.</param>
    /// <param name="logger">Optional logger. Defaults to <see cref="MqttNetNullLogger.Instance"/>.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that detaches the handler and stops the scheduler.</returns>
    public static IAsyncDisposable UseDelayedPublish(
        this MqttServer server,
        DelayedPublishOptions options = null,
        IDelayedMessageStore store = null,
        IMqttNetLogger logger = null)
    {
        ArgumentNullException.ThrowIfNull(server);

        options ??= new DelayedPublishOptions();
        store ??= new InMemoryDelayedMessageStore();
        logger ??= MqttNetNullLogger.Instance;

        var scheduler = new DelayedMessageScheduler(server, store, options, logger);
        var interceptor = new DelayedPublishInterceptor(options, scheduler, logger);

        Func<InterceptingPublishEventArgs, Task> handler = interceptor.OnInterceptingPublishAsync;
        server.InterceptingPublishAsync += handler;

        // Start the scheduler (rehydration happens here). This API is synchronous by design; the
        // scheduler's StartAsync only performs a store load.
        _ = scheduler.StartAsync(CancellationToken.None);

        return new Registration(server, handler, scheduler);
    }

    sealed class Registration : IAsyncDisposable
    {
        readonly MqttServer _server;
        readonly Func<InterceptingPublishEventArgs, Task> _handler;
        readonly DelayedMessageScheduler _scheduler;
        int _disposed;

        public Registration(MqttServer server, Func<InterceptingPublishEventArgs, Task> handler, DelayedMessageScheduler scheduler)
        {
            _server = server;
            _handler = handler;
            _scheduler = scheduler;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _server.InterceptingPublishAsync -= _handler;
            await _scheduler.DisposeAsync().ConfigureAwait(false);
        }
    }
}
