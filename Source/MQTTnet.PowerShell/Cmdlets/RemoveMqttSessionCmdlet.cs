using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.Remove, "MqttSession")]
public class RemoveMqttSessionCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    protected override void ProcessRecord()
    {
        Session.Dispose();
        WriteObject("Session removed.");
    }
}