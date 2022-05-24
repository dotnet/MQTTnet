// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public static class TaskExtensions
    {
        public static void RunInBackground(this Task task, MqttNetSourceLogger logger = null)
        {
            task?.ContinueWith(t =>
                {
                    // Consume the exception first so that we get no exception regarding the not observed exception.
                    var exception = t.Exception;
                    logger?.Error(exception, "Unhandled exception in background task.");
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
