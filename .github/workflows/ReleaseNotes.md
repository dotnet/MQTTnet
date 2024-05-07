* [Core] Optimized packet serialization of PUBACK and PUBREC packets for protocol version 5.0.0 (#1939, thanks to @Y-Sindo).
* [Core] The package inspector is now fully async (#1941).
* [Client] Added a dedicated exception when the client is not connected (#1954, thanks to @marcpiulachs).
* [Client] The client will now throw a _MqttClientUnexpectedDisconnectReceivedException_ when publishing a QoS 0 message which leads to a server disconnect (BREAKING CHANGE!, #1974, thanks to @fazho).
* [Client] Exposed the certificate selection event handler in client options (#1984).
* [Server] The server will no longer send _NoMatchingSubscribers_ when the actual subscription was non success (#1965, BREAKING CHANGE!).
* [Server] Fixed broken support for _null_ in _AddServer_ method in ASP.NET integration (#1981).
* [ManagedClient] Added a new event (SubscriptionsChangedAsync) which is fired when a subscription or unsubscription was made (#1894, thanks to @pdufrene).
* [ManagedClient] Fixed race condition when server shuts down while subscribing (#1987, thanks to @marve).
* [TopicTemplate] Added new extension which provides a template engine for topics (#1932, thanks to @simonthum).
