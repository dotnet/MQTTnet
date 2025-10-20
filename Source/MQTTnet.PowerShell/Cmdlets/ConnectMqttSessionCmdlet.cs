using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommunications.Connect, "MqttSession")]
[OutputType(typeof(MqttClientConnectResult))]
public class ConnectMqttSessionCmdlet : PSCmdlet
{
    [Parameter]
    public SwitchParameter CleanSession { get; set; } = true;

    [Parameter]
    public string? ClientId { get; set; } = Guid.NewGuid().ToString();

    [Parameter(Mandatory = true)]
    public required new string Host { get; set; }

    [Parameter]
    public string? Password { get; set; }

    [Parameter]
    public int Port { get; set; } = 1883;

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    public string? Username { get; set; }

    [Parameter]
    public SwitchParameter UseTls { get; set; }

    protected override void ProcessRecord()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder();
        clientOptionsBuilder.WithTcpServer(Host, Port);

        if (Username != null)
        {
            clientOptionsBuilder.WithCredentials(Username, Password);
        }

        var response = Session.GetClient().ConnectAsync(clientOptionsBuilder.Build()).GetAwaiter().GetResult();

        WriteObject(response);
    }
}