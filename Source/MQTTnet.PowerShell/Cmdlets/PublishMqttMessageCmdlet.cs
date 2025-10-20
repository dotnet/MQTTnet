using System.Management.Automation;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsData.Publish, "MqttMessage")]
public class PublishMqttMessageCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true)]
    public string? Payload { get; set; }

    [Parameter]
    public int QoS { get; set; } = 0;

    [Parameter]
    public SwitchParameter Retain { get; set; }

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required MqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    protected override void ProcessRecord()
    {
        // if (Session == null || !Session.IsConnected)
        //     throw new InvalidOperationException("Session not connected.");

        var msg = new MqttApplicationMessageBuilder().WithTopic(Topic)
            .WithPayload(Encoding.UTF8.GetBytes(Payload ?? string.Empty))
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)QoS)
            .WithRetainFlag(Retain)
            .Build();

        var response = Session.Client.PublishAsync(msg).GetAwaiter().GetResult();

        WriteObject(response);
    }
}