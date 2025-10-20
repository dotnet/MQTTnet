using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommunications.Disconnect, "MqttSession")]
public class DisconnectMqttSessionCmdlet : PSCmdlet
{
    [Parameter]
    public MqttClientDisconnectOptionsReason Reason { get; set; } = MqttClientDisconnectOptionsReason.NormalDisconnection;

    [Parameter]
    public string? ReasonString { get; set; }

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    public uint SessionExpiryInterval { get; set; }

    protected override void ProcessRecord()
    {
        if (Session.GetClient().IsConnected)
        {
            var options = new MqttClientDisconnectOptionsBuilder().WithSessionExpiryInterval(SessionExpiryInterval).WithReason(Reason).WithReasonString(ReasonString).Build();

            Session.GetClient().DisconnectAsync(options).GetAwaiter().GetResult();
        }

        WriteObject("Disconnected.");
    }
}