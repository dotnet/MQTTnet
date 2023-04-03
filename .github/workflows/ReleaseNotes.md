* [Core] Added net7.0 builds (#1635, thanks to @YAJeff).
* [Core] Added support for payload segments (#1585, thanks to @xljiulang)
* [Client] Added support for passing MQTT v5 options (User properties etc.) for disconnects.
* [Client] An internal exception is no longer caught silently when calling _DisconnectAsync_ to indicate that the disconnect is not clean (BREAKING CHANGE).
* [Client] MQTTv5 features are now checked and an exception is thrown if they are used when using protocol version 3.1.1 and lower. These checks can be disabled in client options. (BREAKING CHANGE!).
* [Client] Added support for disabling packet fragmentation (required for i.e. AWS, #1690, thanks to @logicaloud).
* [Server] Exposed MQTT v5 sent properties from the affected client in _ClientDisconnectedAsync_ event.
* [Server] Fixed wrong client ID passed to _InterceptingUnsubscriptionEventArgs_ (#1631, thanks to @ghord). 
* [Server] Exposed socket settings for TCP keep alive in TCP options (#1544).
* [Server] Exposed server session items at server level and allow custom session items for injected application messages (#1638).
* [Server] Improved performance for retained message handling when subscribing using "DoNotSendOnSubscribe" or "SendAtSubscribeIfNewSubscriptionOnly" (#1661, thanks to @Int32Overflow).
* [Server] Added support for changing the used TLS certificate while the server is running (#1652, thanks to @YAJeff). The certificate provider will now be invoked for every new connection!
* [Server] Added a new API method which allows reading a single retained message without the need to processing the entire set of retained messages (#1659).
* [Server] Added a new event (InterceptingClientEnqueueAsync) which allows intercepting when an application message is enqueued for a client (#1648).
* [Server] Fixed race condition when handling connections which leads to stopped message transfers (#1677, thanks to @RazvanEmilR).
