// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.Get, "MqttSessionStatus")]
[OutputType(typeof(MqttClientConnectionStatus))]
public class GetMqttSessionStatusCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    protected override void ProcessRecord()
    {
        var client = Session.GetClient();
        var status = new
        {
            IsConnected = client.IsConnected,
            Options = client.Options
        };

        WriteObject(status);
    }
}
