# MQTTnet PowerShell Module

A PowerShell module for MQTTnet that provides cmdlets for MQTT communication in PowerShell.

## Features

- **Full MQTT Protocol Support**: Connect, subscribe, publish, and manage MQTT connections
- **TCP and WebSocket Transports**: Support for both TCP and WebSocket connections
- **TLS/SSL Support**: Secure connections with TLS encryption
- **Async Message Handling**: Register PowerShell ScriptBlocks as message handlers
- **Advanced MQTT Features**: 
  - Quality of Service (QoS) levels 0, 1, and 2
  - Retained messages
  - Last Will and Testament (LWT)
  - User properties
  - Message expiry intervals
  - Response topics

## Installation

1. Build the MQTTnet.PowerShell project:
   ```powershell
   dotnet build Source/MQTTnet.PowerShell/MQTTnet.PowerShell.csproj
   ```

2. Import the module:
   ```powershell
   Import-Module ./Source/MQTTnet.PowerShell/bin/Debug/net8.0/MQTTnet.PowerShell.dll
   ```

## Quick Start

### Basic Connection and Publish

```powershell
# Create a new MQTT session
$session = New-MqttSession

# Connect to a broker
Connect-MqttSession -Session $session -Host "broker.hivemq.com" -Port 1883

# Publish a message
Publish-MqttMessage -Session $session -Topic "test/topic" -Payload "Hello MQTT!"

# Disconnect and cleanup
Disconnect-MqttSession -Session $session
Remove-MqttSession -Session $session
```

### Subscribe and Receive Messages

```powershell
$session = New-MqttSession
Connect-MqttSession -Session $session -Host "broker.hivemq.com"

# Subscribe to a topic
Subscribe-MqttTopic -Session $session -Topic "test/demo"

# Wait for a message (with timeout)
$message = Receive-MqttMessage -Session $session -TimeoutSeconds 30
Write-Host "Received: $($message.Payload)"

Disconnect-MqttSession -Session $session
Remove-MqttSession -Session $session
```

### Async Message Handler

```powershell
$session = New-MqttSession
Connect-MqttSession -Session $session -Host "broker.hivemq.com"

Subscribe-MqttTopic -Session $session -Topic "test/#"

# Register a message handler
Register-MqttMessageHandler -Session $session -Action {
    param($topic, $payload, $message)
    Write-Host "Message on $topic : $payload"
    Write-Host "QoS: $($message.QoS), Retain: $($message.Retain)"
}

# Messages will be handled asynchronously
Start-Sleep -Seconds 60

Disconnect-MqttSession -Session $session
Remove-MqttSession -Session $session
```

## Available Cmdlets

### Help and Examples

#### Show-MqttExamples
Displays quick examples for common MQTT tasks.

```powershell
Show-MqttExamples
```

### Session Management

#### New-MqttSession
Creates a new MQTT session.

```powershell
$session = New-MqttSession
```

#### Connect-MqttSession
Connects to an MQTT broker with various options.

**TCP Connection:**
```powershell
Connect-MqttSession -Session $session -Host "broker.hivemq.com" -Port 1883
```

**TCP with TLS:**
```powershell
Connect-MqttSession -Session $session -Host "broker.hivemq.com" -Port 8883 `
    -UseTls -AllowUntrustedCertificates
```

**WebSocket Connection:**
```powershell
Connect-MqttSession -Session $session -WebSocketUri "broker.hivemq.com:8000/mqtt"
```

**With Authentication:**
```powershell
Connect-MqttSession -Session $session -Host "broker.hivemq.com" `
    -Username "user" -Password "pass"
```

**With Last Will and Testament:**
```powershell
Connect-MqttSession -Session $session -Host "broker.hivemq.com" `
    -WillTopic "status/client" -WillPayload "Offline" -WillQoS 1 -WillRetain
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `Host`: Broker hostname (for TCP)
- `Port`: Broker port (default: 1883)
- `WebSocketUri`: WebSocket URI (for WebSocket connections)
- `Username`: Username for authentication
- `Password`: Password for authentication
- `ClientId`: Client identifier (auto-generated if not specified)
- `CleanSession`: Use clean session (default: true)
- `UseTls`: Enable TLS encryption
- `AllowUntrustedCertificates`: Allow untrusted certificates
- `KeepAlivePeriod`: Keep-alive period in seconds (default: 15)
- `Timeout`: Connection timeout in seconds (default: 10)
- `WillTopic`: Last Will topic
- `WillPayload`: Last Will payload
- `WillQoS`: Last Will QoS level (0-2)
- `WillRetain`: Retain Last Will message

#### Get-MqttSessionStatus
Gets the current status of the MQTT session.

```powershell
$status = Get-MqttSessionStatus -Session $session
Write-Host "Connected: $($status.IsConnected)"
```

#### Disconnect-MqttSession
Disconnects from the MQTT broker.

```powershell
Disconnect-MqttSession -Session $session
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `Reason`: Disconnect reason code
- `ReasonString`: Human-readable reason
- `SessionExpiryInterval`: Session expiry interval

#### Remove-MqttSession
Disposes the MQTT session and releases resources.

```powershell
Remove-MqttSession -Session $session
```

#### Test-MqttConnection
Tests if the MQTT connection is alive by sending a PING packet.

```powershell
$isAlive = Test-MqttConnection -Session $session -TimeoutSeconds 5
Write-Host "Connection is alive: $isAlive"
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `TimeoutSeconds`: Timeout for the ping operation (default: 5)

### Publishing

#### Publish-MqttMessage
Publishes a message to a topic.

**Basic:**
```powershell
Publish-MqttMessage -Session $session -Topic "test/topic" -Payload "Hello"
```

**With QoS and Retain:**
```powershell
Publish-MqttMessage -Session $session -Topic "test/topic" -Payload "Hello" `
    -QoS 1 -Retain
```

**With User Properties:**
```powershell
$props = @{
    "source" = "PowerShell"
    "version" = "1.0"
}
Publish-MqttMessage -Session $session -Topic "test/topic" -Payload "Hello" `
    -UserProperties $props
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `Topic`: Topic to publish to (mandatory)
- `Payload`: Message payload (mandatory)
- `QoS`: Quality of Service level (0-2, default: 0)
- `Retain`: Retain flag
- `ContentType`: Content type of the payload
- `ResponseTopic`: Response topic for request/response pattern
- `UserProperties`: Hashtable of user properties
- `TopicAlias`: Topic alias
- `MessageExpiryInterval`: Message expiry interval in seconds

### Subscribing

#### Subscribe-MqttTopic
Subscribes to a topic.

```powershell
Subscribe-MqttTopic -Session $session -Topic "test/demo" -QoS 1
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `Topic`: Topic filter to subscribe to (mandatory)
- `QoS`: Quality of Service level (0-2, default: 0)
- `NoLocal`: No local flag
- `RetainAsPublished`: Retain as published flag
- `RetainHandling`: Retain handling option
- `SubscriptionIdentifier`: Subscription identifier

#### Unsubscribe-MqttTopic
Unsubscribes from a topic.

```powershell
Unsubscribe-MqttTopic -Session $session -Topic "test/demo"
```

### Message Handling

#### Receive-MqttMessage
Waits for and receives a single message.

```powershell
# Wait indefinitely
$message = Receive-MqttMessage -Session $session

# Wait with timeout
$message = Receive-MqttMessage -Session $session -TimeoutSeconds 30
```

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `TimeoutSeconds`: Timeout in seconds (0 = no timeout)

#### Register-MqttMessageHandler
Registers an asynchronous message handler using a PowerShell ScriptBlock.

```powershell
Register-MqttMessageHandler -Session $session -Action {
    param($topic, $payload, $message)
    
    Write-Host "Topic: $topic"
    Write-Host "Payload: $payload"
    Write-Host "QoS: $($message.QoS)"
    
    # Access full message properties
    if ($message.UserProperties) {
        foreach ($prop in $message.UserProperties) {
            Write-Host "Property: $($prop.Name) = $($prop.Value)"
        }
    }
}
```

The ScriptBlock receives three parameters:
1. `$topic`: The message topic (string)
2. `$payload`: The message payload as UTF-8 string
3. `$message`: The full message object with properties:
   - `Topic`: Message topic
   - `Payload`: Payload as string
   - `RawPayload`: Payload as byte array
   - `QoS`: Quality of Service level
   - `Retain`: Retain flag
   - `ContentType`: Content type
   - `ResponseTopic`: Response topic
   - `UserProperties`: List of user properties

**Parameters:**
- `Session`: The MQTT session (mandatory)
- `Action`: ScriptBlock to execute for each message (mandatory)

#### Unregister-MqttMessageHandler
Removes all registered message handlers.

```powershell
Unregister-MqttMessageHandler -Session $session
```

## Advanced Examples

### Request-Response Pattern

```powershell
$session = New-MqttSession
Connect-MqttSession -Session $session -Host "broker.hivemq.com"

# Responder
Subscribe-MqttTopic -Session $session -Topic "request/topic"
Register-MqttMessageHandler -Session $session -Action {
    param($topic, $payload, $message)
    if ($message.ResponseTopic) {
        $response = "Response to: $payload"
        Publish-MqttMessage -Session $session -Topic $message.ResponseTopic `
            -Payload $response
    }
}

# Requestor (in another session)
$responseSession = New-MqttSession
Connect-MqttSession -Session $responseSession -Host "broker.hivemq.com"
Subscribe-MqttTopic -Session $responseSession -Topic "response/topic"
Publish-MqttMessage -Session $responseSession -Topic "request/topic" `
    -Payload "Request data" -ResponseTopic "response/topic"

# Wait for response
$response = Receive-MqttMessage -Session $responseSession -TimeoutSeconds 10
```

### High QoS Publishing

```powershell
# Publish with QoS 2 (exactly once delivery)
$result = Publish-MqttMessage -Session $session -Topic "critical/data" `
    -Payload "Important data" -QoS 2

Write-Host "Publish result: $($result.ReasonCode)"
```

### Error Handling in Message Handler

```powershell
Register-MqttMessageHandler -Session $session -Action {
    param($topic, $payload, $message)
    try {
        # Process message
        $data = ConvertFrom-Json $payload
        # Do something with data
    }
    catch {
        Write-Host "Error processing message: $_" -ForegroundColor Red
    }
}
```

## Tips and Best Practices

1. **Always disconnect cleanly**: Use `Disconnect-MqttSession` before `Remove-MqttSession`
2. **Use appropriate QoS levels**: QoS 0 for best performance, QoS 1 for reliability, QoS 2 for critical data
3. **Handle errors in message handlers**: Wrap handler code in try-catch blocks
4. **Use meaningful client IDs**: Set custom client IDs for debugging
5. **Secure connections**: Use TLS for production environments
6. **Topic design**: Use hierarchical topic structures like `device/sensor/temperature`
7. **Clean session**: Use `-CleanSession:$false` to maintain subscriptions across reconnects

## Requirements

- .NET 8.0 or later
- PowerShell 7.0 or later
- MQTTnet library

## License

This project is licensed under the MIT License - see the LICENSE file in the repository root for details.
