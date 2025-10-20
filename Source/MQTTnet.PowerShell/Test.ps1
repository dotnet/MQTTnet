Import-Module ./bin/Debug/net8.0/MQTTnet.PowerShell.dll

$session = New-MqttSession

Connect-MqttSession -Session $session -Host "192.168.1.16"

Publish-MqttMessage -Session $session  -Topic "Test" -Payload "Hello from PowerShell"

Subscribe-MqttTopic -Session $session -Topic "test/demo"

Receive-MqttMessage -Session $session -TimeoutSeconds 200

Disconnect-MqttSession -Session $session
Remove-MqttSession -Session $session

