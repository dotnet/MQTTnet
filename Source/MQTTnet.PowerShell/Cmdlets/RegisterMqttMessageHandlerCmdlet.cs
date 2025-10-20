using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsLifecycle.Register, "MqttMessageHandler")]
public class RegisterMqttMessageHandlerCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required MqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public ScriptBlock? Action { get; set; }

    protected override void ProcessRecord()
    {
        EventHandler<MqttMessage> handler = (s, e) =>
        {
            //throw new NotImplementedException();
            //InvokeCommand.InvokeScript(Action, false, PipelineResultTypes.Output, null, new object[] { e.Topic, e.Payload });
        };

        Session.MessageReceived += handler;
        WriteObject($"Handler registered for MQTT messages on session.");
    }
}