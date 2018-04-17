<p align="center">
<img src="https://github.com/chkr1011/MQTTnet/blob/master/Images/Logo_128x128.png?raw=true" width="128">
</p>

[![NuGet Badge](https://buildstats.info/nuget/MQTTnet)](https://www.nuget.org/packages/MQTTnet)
[![Build status](https://ci.appveyor.com/api/projects/status/ycit86voxfevm2aa/branch/master?svg=true)](https://ci.appveyor.com/project/chkr1011/mqttnet/branch/develop)
[![BCH compliance](https://bettercodehub.com/edge/badge/chkr1011/MQTTnet?branch=master)](https://bettercodehub.com/)

# MQTTnet

MQTTnet is a high performance .NET library for MQTT based communication. It provides a MQTT client and a MQTT server (broker). The implementation is based on the documentation from <http://mqtt.org/>.

## Features

### General

* Async support
* TLS 1.2 support for client and server (but not UWP servers)
* Extensible communication channels (i.e. In-Memory, TCP, TCP+TLS, WS)
* Lightweight (only the low level implementation of MQTT, no overhead)
* Performance optimized (processing ~50.000 messages / second)*
* Interfaces included for mocking and testing
* Access to internal trace messages
* Unit tested (~80 tests)

\* Tested on local machine (Intel i7 8700K) with MQTTnet client and server running in the same process using the TCP channel. The app for verification is part of this repository and stored in _/Tests/MQTTnet.TestApp.NetCore_.

### Client

* Communication via TCP (+TLS) or WS (WebSocket) supported
* Included core _MqttClient_ with low level functionality
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

## Supported frameworks

* .NET Standard 1.3+
* .NET Core 1.1+
* .NET Core App 1.1+
* Universal Windows Platform (UWP) 10.0.10240+ (x86, x64, ARM, AnyCPU, Windows 10 IoT Core)
* .NET Framework 4.5.2+ (x86, x64, AnyCPU)
* Mono 5.2+
* Xamarin.Android 7.5+
* Xamarin.iOS 10.14+

## Supported MQTT versions

* 3.1.1
* 3.1.0

## Nuget

This library is available as a nuget package: <https://www.nuget.org/packages/MQTTnet/>

## Examples

Please find examples and the documentation at the Wiki of this repository (<https://github.com/chkr1011/MQTTnet/wiki>).

## Contributions

If you want to contribute to this project just create a pull request. But only pull requests which are matching the code style of this library will be accepted. Before creating a pull request please have a look at the library to get an overview of the required style.

## References

This library is used in the following projects:

* MQTT Client Rx (Wrapper for Reactive Extensions, <https://github.com/1iveowl/MQTTClient.rx>)
* MQTT Tester (MQTT client test app for [Android](https://play.google.com/store/apps/details?id=com.liveowl.mqtttester) and [iOS](https://itunes.apple.com/us/app/mqtt-tester/id1278621826?mt=8))
* Wirehome (Open Source Home Automation system for .NET, <https://github.com/chkr1011/Wirehome>)


If you use this library and want to see your project here please let me know.

## MIT License

Copyright (c) 2017 Christian Kratky

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
