// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommunications.Receive, "MqttMessage")]
[OutputType(typeof(PsMqttMessage))]
public class ReceiveMqttMessageCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int TimeoutSeconds { get; set; } = 0;

    protected override void ProcessRecord()
    {
        var tcs = new TaskCompletionSource<PsMqttMessage>();
        EventHandler<PsMqttMessage>? handler = null;
        CancellationTokenSource? cts = null;

        try
        {
            handler = (s, e) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetResult(e);
                }
            };

            Session.MessageReceived += handler;

            // Optionaler Timeout
            if (TimeoutSeconds > 0)
            {
                cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
                cts.Token.Register(() =>
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.TrySetCanceled();
                    }
                });
            }

            WriteVerbose("Waiting for next MQTT message...");

            try
            {
                var message = tcs.Task.GetAwaiter().GetResult();
                WriteObject(message);
            }
            catch (TaskCanceledException)
            {
                WriteWarning($"Timeout after {TimeoutSeconds} seconds — no message received.");
            }
        }
        finally
        {
            if (handler != null)
            {
                Session.MessageReceived -= handler;
            }

            cts?.Dispose();
        }
    }
}