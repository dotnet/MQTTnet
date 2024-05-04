* [Core] Optimized packet serialization of PUBACK and PUBREC packets for protocol version 5.0.0 (#1939, thanks to @Y-Sindo).
* [Core] The package inspector is now fully async (#1941).
* [Client] Added a dedicated exception when the client is not connected (#1954, thanks to @marcpiulachs).
* [Client] The client will now throw a _MqttClientUnexpectedDisconnectReceivedException_ when publishing a QoS 0 message which leads to a server disconnect (BREAKING CHANGE!, #1974, thanks to @fazho).
* [ManagedClient] Added a new event (SubscriptionsChangedAsync) which is fired when a subscription or unsubscription was made (#1894, thanks to @pdufrene).
