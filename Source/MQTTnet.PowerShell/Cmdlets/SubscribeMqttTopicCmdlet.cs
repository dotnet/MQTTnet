// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;
using MQTTnet.Protocol;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet("Subscribe", "MqttTopic")]
[OutputType(typeof(MqttClientSubscribeResult))]
public class SubscribeMqttTopicCmdlet : PSCmdlet
{
    [Parameter]
    public bool NoLocal { get; set; }

    [Parameter]
    public int QoS { get; set; } = 0;

    [Parameter]
    public bool RetainAsPublished { get; set; }

    [Parameter]
    public MqttRetainHandling RetainHandling { get; set; } = MqttRetainHandling.SendAtSubscribe;

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    public uint SubscriptionIdentifier { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    protected override void ProcessRecord()
    {
        var topicFilter = new MqttTopicFilterBuilder().WithTopic(Topic)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)QoS)
            .WithNoLocal(NoLocal)
            .WithRetainAsPublished(RetainAsPublished)
            .WithRetainHandling(RetainHandling)
            .Build();

        var options = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter);

        if (SubscriptionIdentifier > 0)
        {
            options.WithSubscriptionIdentifier(SubscriptionIdentifier);
        }

        var response = Session.GetClient().SubscribeAsync(options.Build()).GetAwaiter().GetResult();

        WriteObject(response);
    }
}