// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal;

public sealed class MqttServerKeepAliveMonitor
{
    static readonly MqttServerClientDisconnectOptions DefaultDisconnectOptions = new MqttServerClientDisconnectOptions
    {
        ReasonCode = MqttDisconnectReasonCode.KeepAliveTimeout,
        UserProperties = null,
        ReasonString = null,
        ServerReference = null
    };

    readonly MqttNetSourceLogger _logger;
    readonly MqttServerOptions _options;
    readonly MqttClientSessionsManager _sessionsManager;

    public MqttServerKeepAliveMonitor(MqttServerOptions options, MqttClientSessionsManager sessionsManager, IMqttNetLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(MqttServerKeepAliveMonitor));
    }

    public void Start(CancellationToken cancellationToken)
    {
        // The keep alive monitor spawns a real new thread (LongRunning) because it does not
        // support async/await. Async etc. is avoided here because the thread will usually check
        // the connections every few milliseconds and thus the context changes (due to async) are
        // only consuming resources. Also there is just 1 thread for the entire server which is fine at all!
        Task.Factory.StartNew(_ => DoWork(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning).RunInBackground(_logger);
    }

    void DoWork(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info("Starting keep alive monitor");

            while (!cancellationToken.IsCancellationRequested)
            {
                TryProcessClients();
                Sleep(_options.KeepAliveOptions.MonitorInterval);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Unhandled exception while checking keep alive timeouts");
        }
        finally
        {
            _logger.Verbose("Stopped checking keep alive timeout");
        }
    }

    static void Sleep(TimeSpan timeout)
    {
        try
        {
            Thread.Sleep(timeout);
        }
        catch (ThreadAbortException)
        {
            // The ThreadAbortException is not actively catched in this project.
            // So we use a one which is similar and will be catched properly.
            throw new OperationCanceledException();
        }
    }

    void TryProcessClient(MqttConnectedClient connection, DateTime now)
    {
        try
        {
            if (!connection.IsRunning)
            {
                // The connection is already dead or just created so there is no need to check it.
                return;
            }

            if (connection.ConnectPacket.KeepAlivePeriod == 0)
            {
                // The keep alive feature is not used by the current connection.
                return;
            }

            // Values described here: [MQTT-3.1.2-24].
            // If the client sends 5 sec. the server will allow up to 7.5 seconds.
            // If the client sends 1 sec. the server will allow up to 1.5 seconds.
            var maxSecondsWithoutPacket = connection.ConnectPacket.KeepAlivePeriod * 1.5D;

            var secondsWithoutPackage = (now - connection.Statistics.LastPacketSentTimestamp).TotalSeconds;
            if (secondsWithoutPackage < maxSecondsWithoutPacket)
            {
                // A packet was received before the timeout is affected.
                return;
            }

            _logger.Warning("Client '{0}': Did not receive any packet or keep alive signal", connection.Id);

            // Execute the disconnection in background so that the keep alive monitor can continue
            // with checking other connections.
            // We do not need to wait for the task so no await is needed.
            // Also the internal state of the connection must be swapped to "Finalizing" because the
            // next iteration of the keep alive timer happens.
            _ = connection.StopAsync(DefaultDisconnectOptions);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Client {0}: Unhandled exception while checking keep alive timeouts", connection.Id);
        }
    }

    void TryProcessClients()
    {
        var now = DateTime.UtcNow;
        foreach (var client in _sessionsManager.GetClients())
        {
            TryProcessClient(client, now);
        }
    }
}