<p align="center">
<img src="https://github.com/chkr1011/MQTTnet/blob/master/Images/Logo_128x128.png?raw=true" width="128">
</p>

[![NuGet Badge](https://buildstats.info/nuget/MQTTnet)](https://www.nuget.org/packages/MQTTnet)
[![BCH compliance](https://bettercodehub.com/edge/badge/chkr1011/MQTTnet?branch=master)](https://bettercodehub.com/)

# MQTTnet
MQTTnet is a .NET library for MQTT based communication. It provides a MQTT client and a MQTT server (broker). The implementation is based on the documentation from http://mqtt.org/.

# Features

### General
* Async support
* TLS 1.2 support for client and server (but not UWP servers)
* Extensible communication channels (i.e. In-Memory, TCP, TCP+TLS, WS)
* Lightweight (only the low level implementation of MQTT, no overhead)
* Performance optimized (processing ~27.000 messages / second)*
* Interfaces included for mocking and testing
* Access to internal trace messages
* Unit tested (57+ tests)

\* Tested on local machine with MQTTnet client and server running in the same process using the TCP channel. The app for verification is part of this repository and stored in _/Tests/MQTTnet.TestApp.NetFramework_.

### Client
* Rx support (via another project)
* Communication via TCP (+TLS) or WS (WebSocket)

### Server (broker)
* List of connected clients available
* Supports connected clients with different protocol versions at the same time
* Able to publish its own messages (no loopback client required)
* Able to receive every messages (no loopback client required)
* Extensible client credential validation
* Retained messages are supported including persisting via interface methods (own implementation required)

# Supported frameworks
* .NET Standard 1.3+
* .NET Core 1.1+
* .NET Core App 1.1+
* .NET Framework 4.5.2+ (x86, x64, AnyCPU)
* Universal Windows (UWP) 10.0.10240+ (x86, x64, ARM, AnyCPU)
* Mono 5.2+

# Supported MQTT versions
* 3.1.1
* 3.1.0

# Nuget
This library is available as a nuget package: https://www.nuget.org/packages/MQTTnet/

# Examples
Please find examples and the documentation at the Wiki of this repository (https://github.com/chkr1011/MQTTnet/wiki).

# Contributions
If you want to contribute to this project just create a pull request.

# References
This library is used in the following projects:

* MQTT Client Rx (Wrapper for Reactive Extensions, https://github.com/1iveowl/MQTTClient.rx)
* HA4IoT (Open Source Home Automation system for .NET, https://github.com/chkr1011/HA4IoT)

If you use this library and want to see your project here please let me know.