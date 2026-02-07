// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsCommon.Show, "MqttExamples")]
public class ShowMqttExamplesCmdlet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        var examples = @"
MQTTnet PowerShell Module - Quick Examples
==========================================

1. Basic Connection and Publish:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -Host 'broker.hivemq.com'
   Publish-MqttMessage -Session $session -Topic 'test/topic' -Payload 'Hello'
   Disconnect-MqttSession -Session $session
   Remove-MqttSession -Session $session

2. Subscribe and Receive Messages:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -Host 'broker.hivemq.com'
   Subscribe-MqttTopic -Session $session -Topic 'test/demo'
   $msg = Receive-MqttMessage -Session $session -TimeoutSeconds 30
   Write-Host ""Received: $($msg.Payload)""

3. WebSocket Connection:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -WebSocketUri 'ws://broker.hivemq.com:8000/mqtt'
   Publish-MqttMessage -Session $session -Topic 'test/ws' -Payload 'Hello WS'

4. TLS/SSL Connection:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -Host 'broker.hivemq.com' -Port 8883 `
       -UseTls -AllowUntrustedCertificates

5. Async Message Handler:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -Host 'broker.hivemq.com'
   Subscribe-MqttTopic -Session $session -Topic 'test/#'
   Register-MqttMessageHandler -Session $session -Action {
       param($topic, $payload, $message)
       Write-Host ""Message on $topic : $payload""
   }

6. Last Will and Testament:
   $session = New-MqttSession
   Connect-MqttSession -Session $session -Host 'broker.hivemq.com' `
       -WillTopic 'status/client' -WillPayload 'Offline' -WillQoS 1 -WillRetain

7. Publish with User Properties:
   $props = @{ 'source' = 'PowerShell'; 'version' = '1.0' }
   Publish-MqttMessage -Session $session -Topic 'test/props' `
       -Payload 'Data' -UserProperties $props

8. High QoS Publishing:
   Publish-MqttMessage -Session $session -Topic 'critical/data' `
       -Payload 'Important' -QoS 2

9. Test Connection:
   $isAlive = Test-MqttConnection -Session $session
   Write-Host ""Connection alive: $isAlive""

10. Get Session Status:
    $status = Get-MqttSessionStatus -Session $session
    Write-Host ""Connected: $($status.IsConnected)""

For more details, see: Source/MQTTnet.PowerShell/README.md
";
        
        WriteObject(examples);
    }
}
