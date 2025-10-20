using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet("Unsubscribe", "MqttTopic")]
[OutputType(typeof(MqttClientUnsubscribeResult))]
public class UnsubscribeMqttTopicCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required MqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public required string Topic { get; set; }

    protected override void ProcessRecord()
    {
        var response = Session.Client.UnsubscribeAsync(Topic).GetAwaiter().GetResult();

        WriteObject(response);
    }
}