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
    public required PsMqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    public string? ContentType { get; set; }

    public string? ResponseTopic { get; set; }

    public ushort TopicAlias { get; set; }

    public uint MessageExpiryInterval { get; set; }

    protected override void ProcessRecord()
    {
        // if (Session == null || !Session.IsConnected)
        //     throw new InvalidOperationException("Session not connected.");

        var msg = new MqttApplicationMessageBuilder().WithTopic(Topic)
            .WithPayload(Encoding.UTF8.GetBytes(Payload ?? string.Empty))
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)QoS)
            .WithRetainFlag(Retain)
            .WithContentType(ContentType)
            .WithResponseTopic(ResponseTopic)
            .WithTopicAlias(TopicAlias)
            .WithMessageExpiryInterval(MessageExpiryInterval)
            .Build();

        var response = Session.GetClient().PublishAsync(msg).GetAwaiter().GetResult();

        WriteObject(response);
    }
}