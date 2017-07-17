<p align="center">
<img src="https://github.com/chkr1011/MQTTnet/blob/master/Images/Logo_128x128.png?raw=true" width="128">
</p>

[![NuGet Badge](https://buildstats.info/nuget/MQTTnet)](https://www.nuget.org/packages/MQTTnet)

# MQTTnet
MQTTnet is a .NET library for MQTT based communication. It provides a MQTT client and a MQTT server. The implementation is based on the documentation from http://mqtt.org/.

## Features
* MQTT client included
* MQTT server (broker) included
* TLS 1.2 support for client and server (but not UWP servers)
* Async support
* Rx support (via another project)
* List of connected clients available (server only)
* Extensible communication channels (i.e. In-Memory, TCP, TCP+SSL, WebSockets (not included in this project))
* Access to internal trace messages
* Extensible client credential validation (server only)
* Unit tested (48+ tests)
* Lightweight (only the low level implementation of MQTT, no overhead)

## Supported frameworks
* .NET Standard 1.3+
* .NET Core 1.1+
* .NET Core App 1.1+
* .NET Framework 4.5.2+ (x86, x64, AnyCPU)
* Universal Windows (UWP) 10.0.10240+ (x86, x64, ARM, AnyCPU)

## Supported MQTT versions
* 3.1.1

## Nuget
This library is available as a nuget package: https://www.nuget.org/packages/MQTTnet/

## Contributions
If you want to contribute to this project just create a pull request.

## References
This library is used in the following projects:

* MQTT Client Rx (Wrapper for Reactive Extensions, https://github.com/1iveowl/MQTTClient.rx)
* HA4IoT (Open Source Home Automation system for .NET, https://github.com/chkr1011/HA4IoT)

If you use this library and want to see your project here please let me know.

# MqttClient
## Example

```csharp
var options = new MqttClientOptions
{
    Server = "localhost"
};

var client = new MqttClientFactory().CreateMqttClient(options);
client.ApplicationMessageReceived += (s, e) =>
{
    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
    Console.WriteLine();
};

client.Connected += async (s, e) =>
{
    Console.WriteLine("### CONNECTED WITH SERVER ###");

    await client.SubscribeAsync(new List<TopicFilter>
    {
        new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce)
    });

    Console.WriteLine("### SUBSCRIBED ###");
};

client.Disconnected += async (s, e) => 
{
    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
    await Task.Delay(TimeSpan.FromSeconds(5));

    try
    {
        await client.ConnectAsync();
    }
    catch
    {
        Console.WriteLine("### RECONNECTING FAILED ###");
    }
};

try
{
    await client.ConnectAsync();
}
catch
{
    Console.WriteLine("### CONNECTING FAILED ###");
}

Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

while (true)
{
    Console.ReadLine();

    var applicationMessage = new MqttApplicationMessage(
        "A/B/C",
        Encoding.UTF8.GetBytes("Hello World"),
        MqttQualityOfServiceLevel.AtLeastOnce,
        false
    );

    await client.PublishAsync(applicationMessage);
}
```

# MqttServer

## Example 

```csharp
var options = new MqttServerOptions
{
    ConnectionValidator = p =>
    {
        if (p.ClientId == "SpecialClient")
        {
            if (p.Username != "USER" || p.Password != "PASS")
            {
                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
            }
        }

        return MqttConnectReturnCode.ConnectionAccepted;
    }
};

var mqttServer = new MqttServerFactory().CreateMqttServer(options);
mqttServer.Start();

Console.WriteLine("Press any key to exit.");
Console.ReadLine();

mqttServer.Stop();
```