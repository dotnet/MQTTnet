using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet("Unsubscribe", "MqttTopic")]
[OutputType(typeof(MqttClientUnsubscribeResult))]
public class UnsubscribeMqttTopicCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    protected override void ProcessRecord()
    {
        var options = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(Topic).Build();

        var response = Session.GetClient().UnsubscribeAsync(options).GetAwaiter().GetResult();

        WriteObject(response);
    }
}