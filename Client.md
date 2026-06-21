**These samples are only valid for version 3. Please check the _Samples_ directory for samples and documentation for version 4.0+**

This video tutorial shows how to setup a MQTTnet client for Windows (UWP and IoT Core): https://www.youtube.com/watch?v=PSerr2fvnyc

# Preparation
The following code shows how to create a new MQTT client in the most simple way using the _MqttFactory_.
```csharp
// Create a new MQTT client.
var factory = new MqttFactory();
var mqttClient = factory.CreateMqttClient();
```

# Client options
All options for the MQTT client are bundled in one class named _MqttClientOptions_. It is possible to fill options manually in code via the properties but it is recommended to use the _MqttClientOptionsBuilder_. This class provides a _fluent API_ and allows setting the options easily by providing several overloads and helper methods. The following code shows how to use the builder with several random options.
```csharp
// Create TCP based options using the builder.
var options = new MqttClientOptionsBuilder()
    .WithClientId("Client1")
    .WithTcpServer("broker.hivemq.com")
    .WithCredentials("bud", "%spencer%")
    .WithTls()
    .WithCleanSession()
    .Build();
```

## Securing passwords
The default implementation uses a `string` to hold the client password. However this is a security vulnerability because the password is stored in the heap as clear text. It is recommended to use a `SecureString` for this purpose. But this class is not available for all supported platforms (UWP, netstandard 1.3). This library does not implement it because for other platforms custom implementations like async encryption are required. It is recommended to implement an own _IMqttClientCredentials_ class which returns the decrypted password but does not store it unencrypted.

# TCP connection
The following code shows how to set the options of the MQTT client to make use of a TCP based connection.
```csharp
// Use TCP connection.
var options = new MqttClientOptionsBuilder()
    .WithTcpServer("broker.hivemq.com", 1883) // Port is optional
    .Build();
```

# Secure TCP connection
The following code shows how to use a TLS secured TCP connection (properties are only set as reference):
```csharp
// Use secure TCP connection.
var options = new MqttClientOptionsBuilder()
    .WithTcpServer("broker.hivemq.com")
    .WithTls()
    .Build();
```

# Certificate based authentication
The following code shows how to connect to the server by using certificate based authentication:
```csharp
// Certificate based authentication
List<X509Certificate> certs = new List<X509Certificate>
{
    new X509Certificate2("myCert.pfx")
};

var options = new MqttClientOptionBuilder()
    .WithTcpServer(broker, port)
    .WithTls(new MqttClientOptionsBuilderTlsParameters
    {
        UseTls = true,
        Certificates = certs
    })
    .Build();
```

# Using a Custom CA with TLS
If your server is using a certificate by an unrecognized CA (that is not in the system store), you'll need to add a callback 
to verify if the certificate is valid. Note this does not work prior to .NET 5.

```csharp
// Load the ca.crt. On some platforms you may also be able to use X509Certificate2.Import
X509Certificate2 caCrt = new X509Certificate2(File.ReadAllBytes("ca.crt"));

var options = new MqttClientOptionsBuilder()
    .WithTls(
        new MqttClientOptionsBuilderTlsParameters() {
            UseTls = true,
            SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
            CertificateValidationHandler = (certContext) => {
                X509Chain chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                chain.ChainPolicy.VerificationTime = DateTime.Now;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
                chain.ChainPolicy.CustomTrustStore.Add(caCrt);
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                // convert provided X509Certificate to X509Certificate2
                var x5092 = new X509Certificate2(certContext.Certificate);

                return chain.Build(x5092);
            }
        }
    )
    .Build();

```

# TLS using a client certificate
The following code shows how to connect to the server by using a client certificate based authentication:
```csharp
var caCert = X509Certificate.CreateFromCertFile(@"CA-cert.crt");
var clientCert = new X509Certificate2(@"client-certificate.pfx", "ExportPasswordUsedWhenCreatingPfxFile");

var options = new ManagedMqttClientOptionsBuilder()
    .WithClientOptions(new MqttClientOptionsBuilder()
    .WithClientId(Guid.NewGuid().ToString())
    .WithTcpServer(host, port)
    .WithTls(new MqttClientOptionsBuilderTlsParameters()
        {
            UseTls = true,
            SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
            Certificates = new List<X509Certificate>()
            {
                clientCert, caCert
            }
        })
    .Build())
.Build();
```

The CA certificate is in the *.crt format, the client certificate should be in *.pfx and should have the password that was used to export the file from the private key and certificate originally. The *.pfx file can becreated using openssl as below:

```log
openssl pkcs12 -export -out certificate.pfx -inkey privateKey.key -in clientCertificate.cer
```

# Dealing with special certificates
In order to deal with special certificate errors a special validation callback is available (.NET Framework & netstandard). For UWP apps, a property is available.
```csharp
// For .NET Framework & netstandard apps:
var options = new MqttClientOptionsBuilder()
    .WithTls(new MqttClientOptionsBuilderTlsParameters
    {
        UseTls = true,
        CertificateValidationCallback = (X509Certificate x, X509Chain y, SslPolicyErrors z, IMqttClientOptions o) =>
            {
                // TODO: Check conditions of certificate by using above parameters.
                return true;
            }
    })
    .Build();

// For UWP apps:
MqttTcpChannel.CustomIgnorableServerCertificateErrorsResolver = o =>
{
    if (o.Server == "server_with_revoked_cert")
    {
        return new []{ ChainValidationResult.Revoked };
    }

    return new ChainValidationResult[0];
};
```

# WebSocket connection
In order to use a WebSocket communication channel the following code is required.
```csharp
// Use WebSocket connection.
var options = new MqttClientOptionsBuilder()
    .WithWebSocketServer("broker.hivemq.com:8000/mqtt")
    .Build();
```

Also secure web socket connections can be used via calling the _UseTls()_ method which will switch the protocol from _ws://_ to _wss://_. Usually the sub protocol is required which can be added to the URI directly or to a dedicated property.

# Connecting
After setting up the MQTT client options a connection can be established. The following code shows how to connect with a server. The `CancellationToken.None` can be replaced by a valid [CancellationToken](https://docs.microsoft.com/de-de/dotnet/api/system.threading.cancellationtoken?view=netframework-4.8), of course.
```csharp
// Use WebSocket connection.
var options = new MqttClientOptionsBuilder()
    .WithWebSocketServer("broker.hivemq.com:8000/mqtt")
    .Build();

await mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
```

# Reconnecting
If the connection to the server is lost the _Disconnected_ event is fired. The event is also fired if a call to _ConnectAsync_ has failed because the server is not reachable etc. This allows calling the _ConnectAsync_ method only one time and dealing with retries etc. via consuming the _Disconnected_ event. If the reconnect fails the _Disconnected_ event is fired again. The following code shows how to setup this behavior including a short delay. The `CancellationToken.None` can be replaced by a valid [CancellationToken](https://docs.microsoft.com/de-de/dotnet/api/system.threading.cancellationtoken?view=netframework-4.8), of course.
```csharp
mqttClient.UseDisconnectedHandler(async e =>
{
    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
    await Task.Delay(TimeSpan.FromSeconds(5));

    try
    {
        await mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
    }
    catch
    {
        Console.WriteLine("### RECONNECTING FAILED ###");
    }
});
```
# Consuming messages
The following code shows how to handle incoming messages:
```csharp
mqttClient.UseApplicationMessageReceivedHandler(e =>
{
    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
    Console.WriteLine();

    Task.Run(() => mqttClient.PublishAsync("hello/world"));
});
```
It is also supported to use an async method instead of a synchronized one like in the above example.

⚠️ **Publishing messages inside that received messages handler requires to use _Task.Run_ when using a QoS > 0. The reason is that the message handler has to finish first before the next message is received. The reason is to preserve ordering of the application messages.** 

# Subscribing to a topic
Once a connection with the server is established subscribing to a topic is possible. The following code shows how to subscribe to a topic after the MQTT client has connected.
```csharp
mqttClient.UseConnectedHandler(async e =>
{
    Console.WriteLine("### CONNECTED WITH SERVER ###");

    // Subscribe to a topic
    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("my/topic").Build());

    Console.WriteLine("### SUBSCRIBED ###");
});
```

# Publishing messages
Application messages can be created using the properties directly or via using the _MqttApplicationMessageBuilder_. This class has some useful overloads which allows dealing with different payload formats easily. The API of the builder is a _fluent API_. The following code shows how to compose an application message and publishing them:
```csharp
var message = new MqttApplicationMessageBuilder()
    .WithTopic("MyTopic")
    .WithPayload("Hello World")
    .WithExactlyOnceQoS()
    .WithRetainFlag()
    .Build();

await mqttClient.PublishAsync(message, CancellationToken.None); // Since 3.0.5 with CancellationToken
```
It is not required to fill all properties of an application message. The following code shows how to create a very basic application message:
```csharp
var message = new MqttApplicationMessageBuilder()
    .WithTopic("/MQTTnet/is/awesome")
    .Build();
```

# RPC calls
The extension _MQTTnet.Extensions.Rpc_ (available as nuget) allows sending a request and waiting for the matching reply. This is done via defining a pattern which uses the topic to correlate the request and the response. From client usage it is possible to define a timeout. The following code shows how to send a RPC call.

```csharp
var rpcClient = new MqttRpcClient(_mqttClient);

var timeout = TimeSpan.FromSeconds(5);
var qos = MqttQualityOfServiceLevel.AtMostOnce;

var response = await rpcClient.ExecuteAsync(timeout, "myMethod", payload, qos);
```

The device (Arduino, ESP8266 etc.) which responds to the request needs to parse the topic and reply to it. The following code shows how to implement the handler.

```C
// If using the MQTT client PubSubClient it must be ensured 
// that the request topic for each method is subscribed like the following.
mqttClient.subscribe("MQTTnet.RPC/+/ping");
mqttClient.subscribe("MQTTnet.RPC/+/do_something");

// It is not allowed to change the structure of the topic.
// Otherwise RPC will not work.
// So method names can be separated using an _ or . but no +, # or /.
// If it is required to distinguish between devices
// own rules can be defined like the following:
mqttClient.subscribe("MQTTnet.RPC/+/deviceA.ping");
mqttClient.subscribe("MQTTnet.RPC/+/deviceB.ping");
mqttClient.subscribe("MQTTnet.RPC/+/deviceC.getTemperature");

// Within the callback of the MQTT client the topic must be checked
// if it belongs to MQTTnet RPC. The following code shows one
// possible way of doing this.
void mqtt_Callback(char *topic, byte *payload, unsigned int payloadLength)
{
	String topicString = String(topic);

	if (topicString.startsWith("MQTTnet.RPC/")) {
		String responseTopic = topicString + String("/response");

		if (topicString.endsWith("/deviceA.ping")) {
			mqtt_publish(responseTopic, "pong", false);
			return;
		}
	}
}

// Important notes:
// ! Do not send response message with the _retain_ flag set to true.
// ! All required data for a RPC call and the result must be placed into the payload.
```

# Connecting with Amazon AWS

## MQTTnet AWS Transport Support

dotnet 7 (specifically tested with 7.0.203):

| Provider  | Transport | Status |
| --- | --- | --- |
| dotnet | tcp | ✅ |
| dotnet | websocket | ✅ |
| websocket4net | tcp | ✅ |
| websocket4net | websocket | ✅ |

Example Project: https://github.com/TCROC/aws-iot-custom-auth

Mono (specifically tested with Unity Game Engine 2021.3.24f1)

| Provider  | Transport | Status |
| --- | --- | --- |
| mono | tcp | ❌ |
| mono | websocket | ✅ |
| websocket4net | tcp | ❌ |
| websocket4net | websocket | ✅ |

Example Project: https://github.com/TCROC/mqttnet-unity-alpn.git

^ This project is currently being used to debug TCP as well

## Websockets

AWS Sample:

https://github.com/aws-samples/aws-iot-core-dotnet-app-mqtt-over-websockets-sigv4

## TCP with certificates

### NOTE: The currently tested Unity Game Engine 2021.3.24f1 does not support TCP

1. Remove the Amazon root certificate and just pass the client certificate in the `.pfx` format:

```csharp
List<X509Certificate> certs = new List<X509Certificate>
{
    new X509Certificate2("ClientCertPath", "ClientCertPass", X509KeyStorageFlags.Exportable)
};
```

2. Use the following client code:

```csharp
var clientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(endpoint, port)
    .WithKeepAlivePeriod(new TimeSpan(0, 0, 0, 300))
    .WithTls(new MqttClientOptionsBuilderTlsParameters
    {
        UseTls = true,
#pragma warning disable CS0618 // Type or member is obsolete
        CertificateValidationCallback = (X509Certificate x, X509Chain y, System.Net.Security.SslPolicyErrors z, IMqttClientOptions o) =>
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return true;
        },
        AllowUntrustedCertificates = false,
        IgnoreCertificateChainErrors = false,
        IgnoreCertificateRevocationErrors = false,
        Certificates = certs
    })
    .WithProtocolVersion(MqttProtocolVersion.V311)
    .Build();
```

## Troubleshooting

Before digging dip on certificates, make sure that

* you have the correct URL. Copy it from "Interact" section of the "thing".
* you can connect to that URL using openssl:
```bash
openssl s_client -CAfile awsca.pem -cert xxx.pem.crt -key xxx.pem.key -connect yyy.iot.region.amazonaws.com:8883
```
* the certificate attached to the thing is activated.
* you have attached one policy (or several) that allows the thing to connect, subscribe, publish and receive.
* you're **not** using a feature that isn't supported (Retained messages, will messages, Unsupported QoS 2).

If you're absolutely sure of the above, then start troubleshooting certificates.

## Some small snippets
* Net Framework: https://gist.github.com/jkoplo/16f25875f7aca9503a7520fcda99005f
* NetCore: https://gist.github.com/jkoplo/bd60cfe1a02c6e13b0a2d753289ae00f

# Using the client in ASP.NET Core
When using the client there is no difference between the .Net Framework, .Net Core or ASP.NET Core. The configuration above applies. The client cannot be made available using dependency injection.

A sample example is available here [mqtt-client-dotnet-core](https://github.com/rafiulgits/mqtt-client-dotnet-core)