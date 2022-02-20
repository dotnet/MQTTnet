We have joined the .NET Foundation!

Version 4 comes with a new API so a lot of breaking changes should be expected.
Checkout the upgrade guide (https://github.com/dotnet/MQTTnet/wiki/Upgrading-guide) for an overview of the changes.
Checkout the new samples (https://github.com/dotnet/MQTTnet/tree/feature/master/Samples) how to use the new API. The wiki only remains for version 3 of this library.

* [Core] Improved memory management when working with large payloads.
* [Core] Added support for .NET 6.0.
* [Core] nuget packages are now created by MSBuild including more information (i.e. commit hash).
* [Client] Exposed socket linger state in options.
* [Client] The OS will now choose the best TLS version to use. It is no longer fixed to 1.3 etc. (thanks to @patagonaa, #1271).
* [Client] Added support for _ServerKeepAlive_ (MQTTv5).
* [Client] Exposed user properties and reason string in subscribe result.
* [Client] Exposed user properties and reason string in unsubscribe result.
* [Client] Migrated application message handler to a regular .NET event (BREAKING CHANGE!).
* [Client] The will message is longer a regular application message due to not supported properties by the will message (BREAKING CHANGE!).
* [Client] Timeouts are no longer handled inside the library. Each method (Connect, Publish etc.) supports a cancellation token so that custom timeouts can and must be used (BREAKING CHANGE!). 
* [Server] Exposed socket linger state in options.
* [Server] Added support for returning individual subscription errors (#80 thanks to @jimch)
* [Server] Improved topic filter comparisons (support for $).
* [Server] Added more MQTTv5 response information to all interceptors (BREAKING CHANGE!).
* [Server] Improved session management for MQTT v5 (#1294, thanks to @logicaloud).
* [Server] All interceptors and events are migrated from interfaces to simple events. All existing APIs are availble but must be migrated to corresponding events (BREAKING CHANGE!).
* [Server] Removed all interceptor and event interfaces including the delegate implementations etc. (BREAKING CHANGE!).
* [Server] Renamed a lot of classes and adjusted namespaces (BREAKING CHANGE!).
* [Server] Introduced a new queueing approach for internal message process (packet bus).
* [Server] For security reasons the default endpoint (1883) is no longer enabled by default (BREAKING CHANGE!).