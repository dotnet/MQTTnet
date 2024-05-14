* [Core] Optimized packet serialization of PUBACK and PUBREC packets for protocol version 5.0.0 (#1939, thanks to @Y-Sindo).
* [Core] The package inspector is now fully async (#1941).
* [Client] Exposed the _EndPoint_ type to support other endpoint types (like Unix Domain Sockets) in client options (#1919).
* [Client] Fixed support for unix sockets by exposing more options (#1995).
* [Client] Added a dedicated exception when the client is not connected (#1954, thanks to @marcpiulachs).
* [Client] The client will now throw a _MqttClientUnexpectedDisconnectReceivedException_ when publishing a QoS 0 message which leads to a server disconnect (BREAKING CHANGE!, #1974, thanks to @fazho).
* [Client] Exposed the certificate selection event handler in client options (#1984).
* [Server] Added new events for delivered and dropped messages (#1866, thanks to @kallayj).
* [Server] The server will no longer treat a client which is receiving a large payload as alive. The packet must be received completely within the keep alive boundaries (BREAKING CHANGE!, #1883).
* [Server] Fixed "service not registered" exception in ASP.NET integration (#1889).
* [Server] The server will no longer send _NoMatchingSubscribers_ when the actual subscription was non success (#1965, BREAKING CHANGE!).
* [Server] Fixed broken support for _null_ in _AddServer_ method in ASP.NET integration (#1981).
* [ManagedClient] Added a new event (SubscriptionsChangedAsync) which is fired when a subscription or unsubscription was made (#1894, thanks to @pdufrene).
* [ManagedClient] Fixed race condition when server shuts down while subscribing (#1987, thanks to @marve).
* [TopicTemplate] Added new extension which provides a template engine for topics (#1932, thanks to @simonthum).
