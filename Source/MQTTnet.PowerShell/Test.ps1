# MQTTnet PowerShell Module Test Script
# This script demonstrates the usage of the MQTTnet PowerShell module

Import-Module ./bin/Debug/net8.0/MQTTnet.PowerShell.dll

Write-Host "=== MQTTnet PowerShell Module Test ===" -ForegroundColor Cyan

# Example 1: Basic TCP connection
Write-Host "`n1. Testing basic TCP connection..." -ForegroundColor Yellow
$session = New-MqttSession

try {
    Connect-MqttSession -Session $session -Host "broker.hivemq.com" -Port 1883
    Write-Host "Connected successfully!" -ForegroundColor Green
    
    # Get session status
    Write-Host "`n2. Getting session status..." -ForegroundColor Yellow
    $status = Get-MqttSessionStatus -Session $session
    Write-Host "IsConnected: $($status.IsConnected)" -ForegroundColor Green
    
    # Example 2: Publish a simple message
    Write-Host "`n3. Publishing a simple message..." -ForegroundColor Yellow
    Publish-MqttMessage -Session $session -Topic "test/powershell" -Payload "Hello from PowerShell!"
    Write-Host "Message published!" -ForegroundColor Green
    
    # Example 3: Publish with user properties
    Write-Host "`n4. Publishing with user properties..." -ForegroundColor Yellow
    $properties = @{
        "source" = "PowerShell"
        "version" = "1.0"
    }
    Publish-MqttMessage -Session $session -Topic "test/powershell/advanced" -Payload "Advanced message" -UserProperties $properties -QoS 1
    Write-Host "Message with properties published!" -ForegroundColor Green
    
    # Example 4: Subscribe and receive messages
    Write-Host "`n5. Subscribing to topics..." -ForegroundColor Yellow
    Subscribe-MqttTopic -Session $session -Topic "test/demo"
    Write-Host "Subscribed to test/demo" -ForegroundColor Green
    
    # Example 5: Register a message handler
    Write-Host "`n6. Registering message handler..." -ForegroundColor Yellow
    Register-MqttMessageHandler -Session $session -Action {
        param($topic, $payload, $message)
        Write-Host "Received message on topic: $topic" -ForegroundColor Cyan
        Write-Host "Payload: $payload" -ForegroundColor White
        Write-Host "QoS: $($message.QoS)" -ForegroundColor White
    }
    Write-Host "Handler registered!" -ForegroundColor Green
    
    # Publish a test message that the handler will receive
    Write-Host "`n7. Publishing test message for handler..." -ForegroundColor Yellow
    Publish-MqttMessage -Session $session -Topic "test/demo" -Payload "Test message for handler"
    Start-Sleep -Seconds 2
    
    # Example 6: Unsubscribe
    Write-Host "`n8. Unsubscribing from topic..." -ForegroundColor Yellow
    Unsubscribe-MqttTopic -Session $session -Topic "test/demo"
    Write-Host "Unsubscribed!" -ForegroundColor Green
    
    # Example 7: Clean disconnect
    Write-Host "`n9. Disconnecting..." -ForegroundColor Yellow
    Disconnect-MqttSession -Session $session
    Write-Host "Disconnected!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
finally {
    # Cleanup
    Remove-MqttSession -Session $session
    Write-Host "`nSession cleaned up!" -ForegroundColor Green
}

Write-Host "`n=== WebSocket Connection Example ===" -ForegroundColor Cyan

# Example 8: WebSocket connection
Write-Host "`nTesting WebSocket connection..." -ForegroundColor Yellow
$wsSession = New-MqttSession

try {
    Connect-MqttSession -Session $wsSession -WebSocketUri "broker.hivemq.com:8000/mqtt"
    Write-Host "Connected via WebSocket!" -ForegroundColor Green
    
    Publish-MqttMessage -Session $wsSession -Topic "test/websocket" -Payload "Hello via WebSocket!"
    Write-Host "Message published via WebSocket!" -ForegroundColor Green
    
    Disconnect-MqttSession -Session $wsSession
}
catch {
    Write-Host "WebSocket Error: $_" -ForegroundColor Red
}
finally {
    Remove-MqttSession -Session $wsSession
}

Write-Host "`n=== Connection with Will Message Example ===" -ForegroundColor Cyan

# Example 9: Connection with Last Will Testament
Write-Host "`nTesting connection with Will message..." -ForegroundColor Yellow
$willSession = New-MqttSession

try {
    Connect-MqttSession -Session $willSession -Host "broker.hivemq.com" `
        -WillTopic "test/will" -WillPayload "Client disconnected unexpectedly" -WillQoS 1 -WillRetain
    Write-Host "Connected with Will message configured!" -ForegroundColor Green
    
    Disconnect-MqttSession -Session $willSession
}
catch {
    Write-Host "Will Error: $_" -ForegroundColor Red
}
finally {
    Remove-MqttSession -Session $willSession
}

Write-Host "`n=== All tests completed ===" -ForegroundColor Cyan

