// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsLifecycle.Unregister, "MqttMessageHandler")]
public class UnregisterMqttMessageHandlerCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    protected override void ProcessRecord()
    {
        // Clear all event handlers
        Session.ClearMessageHandlers();
        WriteObject($"All message handlers cleared for MQTT session.");
    }
}
