* [Core] Improved memory management when working with large payloads.
* [Core] Added support for .NET 6.0.
* [Core] nuget packages are now created by MSBuild including more information (i.e. commit hash).
* [Client] The OS will now choose the best TLS version to use. It is no longer fixed to 1.3 etc. (thanks to @patagonaa, #1271).
* [Client] Exposed user properties and reason string in subscribe result.
* [Client] Exposed user properties and reason string in unsubscribe result.
* [Server] Added support for returning individual subscription errors (#80 thanks to @jimch)
* [Server] Improved topic filter comparisons (support for $).
* [Server] Added more MQTTv5 response information to all interceptors (BREAKING CHANGE!).
* [Server] Improved session management for MQTT v5 (#1294, thanks to @logicaloud).
* [Server] All interceptors and events are migrated from interfaces to simple events. All existing APIs are availble but must be migrated to corresponding events (BREAKING CHANGE!).
* [Server] Removed all interceptor and event interfaces including the delegate implementations etc. (BREAKING CHANGE!).
* [Server] Renamed a lot of classes and adjsuted namespaces (BREAKING CHANGE!).
* [Server] Introduced a new queueing approach for internal message process (packet bus).