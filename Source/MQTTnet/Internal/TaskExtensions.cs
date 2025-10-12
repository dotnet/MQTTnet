// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Internal;

public static class TaskExtensions
{
    public static void RunInBackground(this Task task, MqttNetSourceLogger logger = null)
    {
        task?.ContinueWith(
            t =>
            {
                // Consume the exception first so that we get no exception regarding the not observed exception.
                var exception = t.Exception;
                logger?.Error(exception, "Unhandled exception in background task.");
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public static async Task WaitAsync(this Task task, Task sender, MqttNetSourceLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (task == null)
        {
            return;
        }

        if (task == sender)
        {
            // Return here to avoid deadlocks, but first any eventual exception in the task
            // must be handled to avoid not getting an unhandled task exception
            if (!task.IsFaulted)
            {
                return;
            }

            // By accessing the Exception property the exception is considered handled and will
            // not result in an unhandled task exception later by the finalizer
            logger.Warning(task.Exception, "Error while waiting for background task.");
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}