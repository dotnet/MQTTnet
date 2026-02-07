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

    [Parameter(Mandatory = true, ParameterSetName = "Tcp")]
    [Parameter(Mandatory = true, ParameterSetName = "TcpWithTls")]
    public new string? Host { get; set; }

    [Parameter]
    public string? Password { get; set; }

    [Parameter(ParameterSetName = "Tcp")]
    [Parameter(ParameterSetName = "TcpWithTls")]
    public int Port { get; set; } = 1883;

    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter]
    public string? Username { get; set; }

    [Parameter(ParameterSetName = "TcpWithTls")]
    public SwitchParameter UseTls { get; set; }

    [Parameter(ParameterSetName = "TcpWithTls")]
    public SwitchParameter AllowUntrustedCertificates { get; set; }

    [Parameter(Mandatory = true, ParameterSetName = "WebSocket")]
    public string? WebSocketUri { get; set; }

    [Parameter]
    public int KeepAlivePeriod { get; set; } = 15;

    [Parameter]
    public int Timeout { get; set; } = 10;

    [Parameter]
    public string? WillTopic { get; set; }

    [Parameter]
    public string? WillPayload { get; set; }

    [Parameter]
    public int WillQoS { get; set; } = 0;

    [Parameter]
    public SwitchParameter WillRetain { get; set; }

    protected override void ProcessRecord()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder();
        
        // Configure transport
        if (ParameterSetName == "WebSocket")
        {
            clientOptionsBuilder.WithWebSocketServer(o => o.WithUri(WebSocketUri!));
        }
        else
        {
            clientOptionsBuilder.WithTcpServer(Host!, Port);
            
            if (UseTls)
            {
                clientOptionsBuilder.WithTlsOptions(o =>
                {
                    if (AllowUntrustedCertificates)
                    {
                        o.WithCertificateValidationHandler(_ => true);
                    }
                });
            }
        }

        // Configure authentication
        if (Username != null)
        {
            clientOptionsBuilder.WithCredentials(Username, Password);
        }

        // Configure session
        clientOptionsBuilder.WithCleanSession(CleanSession);
        
        if (!string.IsNullOrEmpty(ClientId))
        {
            clientOptionsBuilder.WithClientId(ClientId);
        }

        // Configure keep alive
        clientOptionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(KeepAlivePeriod));
        clientOptionsBuilder.WithTimeout(TimeSpan.FromSeconds(Timeout));

        // Configure Will message
        if (!string.IsNullOrEmpty(WillTopic))
        {
            clientOptionsBuilder.WithWillTopic(WillTopic);
            
            if (!string.IsNullOrEmpty(WillPayload))
            {
                clientOptionsBuilder.WithWillPayload(System.Text.Encoding.UTF8.GetBytes(WillPayload));
            }
            
            clientOptionsBuilder.WithWillQualityOfServiceLevel((Protocol.MqttQualityOfServiceLevel)WillQoS);
            clientOptionsBuilder.WithWillRetain(WillRetain);
        }

        var response = Session.GetClient().ConnectAsync(clientOptionsBuilder.Build()).GetAwaiter().GetResult();

        WriteObject(response);
    }
}