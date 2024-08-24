<p align="center">
<img src="https://github.com/dotnet/MQTTnet/blob/master/Images/icon_det_256.png?raw=true" width="196">
<br/>
</p>

[![NuGet Badge](https://buildstats.info/nuget/MQTTnet)](https://www.nuget.org/packages/MQTTnet)
[![CI](https://github.com/dotnet/MQTTnet/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/dotnet/MQTTnet/actions/workflows/ci.yml)
[![MyGet](https://img.shields.io/myget/mqttnet/v/mqttnet?color=orange&label=MyGet-Preview)](https://www.myget.org/feed/mqttnet/package/nuget/MQTTnet)
![Size](https://img.shields.io/github/repo-size/dotnet/MQTTnet.svg)
[![Join the chat at https://gitter.im/MQTTnet/community](https://badges.gitter.im/MQTTnet/community.svg)](https://gitter.im/MQTTnet/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://raw.githubusercontent.com/dotnet/MQTTnet/master/LICENSE)

# MQTTnet

MQTTnet is a high performance .NET library for MQTT based communication. It provides a MQTT client and a MQTT server (
broker) and supports the MQTT protocol up to version 5. It is compatible with mostly any supported .NET Framework
version and CPU architecture.

## Features

### General

* Async support
* TLS support for client and server (but not UWP servers)
* Extensible communication channels (e.g. In-Memory, TCP, TCP+TLS, WS)
* Lightweight (only the low level implementation of MQTT, no overhead)
* Performance optimized (processing ~150.000 messages / second)*
* Uniform API across all supported versions of the MQTT protocol
* Access to internal trace messages
* Unit tested (~636 tests)
* No external dependencies

\* Tested on local machine (Intel i7 8700K) with MQTTnet client and server running in the same process using the TCP
channel. The app for verification is part of this repository and stored in _/Tests/MQTTnet.TestApp.NetCore_.

### Client

* Communication via TCP (+TLS) or WS (WebSocket) supported
* Included core _LowLevelMqttClient_ with low level functionality
* Also included _ManagedMqttClient_ which maintains the connection and subscriptions automatically. Also application
  messages are queued and re-scheduled for higher QoS levels automatically.
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

## Getting Started

MQTTnet is delivered via <b>NuGet</b> package manager. You can find the packages
here: https://www.nuget.org/packages/MQTTnet/

Use these command in the Package Manager console to install MQTTnet manually:

```
Install-Package MQTTnet
```

Samples for using MQTTnet are part of this repository. For starters these samples are recommended:

- [Connect with a broker](https://github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Connection_Samples.cs)
- [Subscribing to data](https://github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Subscribe_Samples.cs)
- [Publishing data](https://github.com/dotnet/MQTTnet/blob/master/Samples/Client/Client_Publish_Samples.cs)
- [Host own broker](https://github.com/dotnet/MQTTnet/blob/master/Samples/Server/Server_Simple_Samples.cs)

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our
community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
