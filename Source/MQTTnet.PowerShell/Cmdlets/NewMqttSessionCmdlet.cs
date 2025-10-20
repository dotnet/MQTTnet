using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.New, "MqttSession")]
[OutputType(typeof(MqttSession))]
public class NewMqttSessionCmdlet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        WriteObject(new MqttSession());
    }
}