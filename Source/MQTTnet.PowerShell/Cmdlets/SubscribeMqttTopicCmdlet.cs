// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet("Subscribe", "MqttTopic")]
[OutputType(typeof(MqttClientSubscribeResult))]
public class SubscribeMqttTopicCmdlet : PSCmdlet
{
    [Parameter]
    public int QoS { get; set; } = 0;

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required MqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    protected override void ProcessRecord()
    {
        var topicFilter = new MqttTopicFilterBuilder().WithTopic(Topic).Build();

        var response = Session.Client.SubscribeAsync(topicFilter).GetAwaiter().GetResult();

        WriteObject(response);
    }
}