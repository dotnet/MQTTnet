using System.Collections;
using System.Management.Automation;
using System.Text;
using MQTTnet.Protocol;
using MQTTnet.Packets;

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

    [Parameter]
    public string? ContentType { get; set; }

    [Parameter]
    public string? ResponseTopic { get; set; }

    [Parameter]
    public ushort TopicAlias { get; set; }

    [Parameter]
    public uint MessageExpiryInterval { get; set; }

    [Parameter]
    public Hashtable? UserProperties { get; set; }

    protected override void ProcessRecord()
    {
        var msgBuilder = new MqttApplicationMessageBuilder()
            .WithTopic(Topic)
            .WithPayload(Encoding.UTF8.GetBytes(Payload ?? string.Empty))
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)QoS)
            .WithRetainFlag(Retain)
            .WithContentType(ContentType)
            .WithResponseTopic(ResponseTopic)
            .WithTopicAlias(TopicAlias)
            .WithMessageExpiryInterval(MessageExpiryInterval);

        // Add user properties if provided
        if (UserProperties != null && UserProperties.Count > 0)
        {
            foreach (var key in UserProperties.Keys)
            {
                var value = UserProperties[key]?.ToString() ?? string.Empty;
                msgBuilder.WithUserProperty(key.ToString()!, value);
            }
        }

        var msg = msgBuilder.Build();
        var response = Session.GetClient().PublishAsync(msg).GetAwaiter().GetResult();

        WriteObject(response);
    }
}