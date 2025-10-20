using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.New, "MqttSession")]
[OutputType(typeof(PsMqttSession))]
public class NewMqttSessionCmdlet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        WriteObject(new PsMqttSession());
    }
}