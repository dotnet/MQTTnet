<p align="center">
<img src="https://github.com/chkr1011/MQTTnet/blob/master/Images/icon_det_256.png?raw=true" width="196">
<br/>
<br/>
</p>

[![NuGet Badge](https://buildstats.info/nuget/MQTTnet)](https://www.nuget.org/packages/MQTTnet)
[![CI](https://github.com/dotnet/MQTTnet/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/MQTTnet/actions/workflows/ci.yml)
[![Join the chat at https://gitter.im/MQTTnet/community](https://badges.gitter.im/MQTTnet/community.svg)](https://gitter.im/MQTTnet/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://raw.githubusercontent.com/dotnet/MQTTnet/master/LICENSE)

# MQTTnet

MQTTnet is a high performance .NET library for MQTT based communication. It provides a MQTT client and a MQTT server (broker) and supports the MQTT protocol up to version 5.

## Features

### General

* Async support
* TLS support for client and server (but not UWP servers)
* Extensible communication channels (e.g. In-Memory, TCP, TCP+TLS, WS)
* Lightweight (only the low level implementation of MQTT, no overhead)
* Performance optimized (processing ~70.000 messages / second)*
* Uniform API across all supported versions of the MQTT protocol
* Interfaces included for mocking and testing
* Access to internal trace messages
* Unit tested (~636 tests)
* No external dependencies

\* Tested on local machine (Intel i7 8700K) with MQTTnet client and server running in the same process using the TCP channel. The app for verification is part of this repository and stored in _/Tests/MQTTnet.TestApp.NetCore_.

### Client

* Communication via TCP (+TLS) or WS (WebSocket) supported
* Included core _LowLevelMqttClient_ with low level functionality
* Also included _ManagedMqttClient_ which maintains the connection and subscriptions automatically. Also application messages are queued and re-scheduled for higher QoS levels automatically.
* Rx support (via another project)
* Compatible with Microsoft Azure IoT Hub

### Server (broker)

* List of connected clients available
* Supports connected clients with different protocol versions at the same time
* Able to publish its own messages (no loopback client required)
* Able to receive every message (no loopback client required)
* Extensible client credential validation
* Retained messages are supported including persisting via interface methods (own implementation required)
* WebSockets supported (via ASP.NET Core 2.0, separate nuget)
* A custom message interceptor can be added which allows transforming or extending every received application message
* Validate subscriptions and deny subscribing of certain topics depending on requesting clients
* Connect clients with different protocol versions at the same time.

## Supported frameworks

| Framwork           | Version     |
| ------------------ | ----------- |
|.NET                | 5.0+        |
|.NET Framework      | 4.5.2+      |
|.NET Standard       | 1.3+        |
|.NET Core           | 1.1+        |
|.NET Core App       | 1.1+        |
| Mono               | 5.2+        |
| UWP                | 10.0.10240+ |
| Xamarin.Android    | 7.5+        |
| Xamarin.iOS        | 10.14+      |
| Blazor WebAssembly | 3.2.0+      |

## References

This library is used in the following projects:

* Azure Functions MQTT Bindings, <https://github.com/keesschollaart81/CaseOnline.Azure.WebJobs.Extensions.Mqtt>)
* HA4IoT (Open Source Home Automation system for .NET, <https://github.com/chkr1011/HA4IoT>)
* MQTT Client Rx (Wrapper for Reactive Extensions, <https://github.com/1iveowl/MQTTClient.rx>)
* MQTT Client Rx (Managed Client Wrapper for Reactive Extensions, <https://github.com/mmuecke/RxMQTTnet>)
* MQTT Tester (MQTT client test app for [Android](https://play.google.com/store/apps/details?id=com.liveowl.mqtttester) and [iOS](https://itunes.apple.com/us/app/mqtt-tester/id1278621826?mt=8))
* MQTTnet App (Cross platform client application for MQTT debugging, inspection etc., <https://github.com/chkr1011/MQTTnet.App>)
* Wirehome.Core (Open Source Home Automation system for .NET Core, <https://github.com/chkr1011/Wirehome.Core>)
* SparkplugNet (Sparkplug library for .Net, <https://github.com/SeppPenner/SparkplugNet>)
* Silverback (Framework to build event-driven applications - support for MQTT, Kafka & RabbitMQ) <https://github.com/BEagle1984/silverback>

Further projects using this project can be found under https://github.com/dotnet/MQTTnet/network/dependents.

If you use this library and want to see your project here please create a pull request.