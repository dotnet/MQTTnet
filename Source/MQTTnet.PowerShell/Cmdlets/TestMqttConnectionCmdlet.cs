// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsDiagnostic.Test, "MqttConnection")]
[OutputType(typeof(bool))]
public class TestMqttConnectionCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    public int TimeoutSeconds { get; set; } = 5;

    protected override void ProcessRecord()
    {
        try
        {
            var client = Session.GetClient();
            
            if (!client.IsConnected)
            {
                WriteObject(false);
                return;
            }

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
            client.PingAsync(cts.Token).GetAwaiter().GetResult();
            
            WriteObject(true);
        }
        catch
        {
            WriteObject(false);
        }
    }
}
