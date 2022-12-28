* [Core] Added net7.0 builds (#1635, thanks to @YAJeff).
* [Client] Added support for passing MQTT v5 options (User properties etc.) for disconnects.
* [Client] An internal exception is no longer caught silently when calling _DisconnectAsync_ to indicate that the disconnect is not clean (BREAKING CHANGE).
* [Client] MQTTv5 features are now checked and an exception is thrown if they are used when using protocol version 3.1.1 and lower. These checks can be disabled in client options. (BREAKING CHANGE!).
* [Server] Exposed MQTT v5 sent properties from the affected client in _ClientDisconnectedAsync_ event.
* [Server] Fixed wrong client ID passed to _InterceptingUnsubscriptionEventArgs_ (#1631, thanks to @ghord). 
* [Server] Exposed socket settings for TCP keep alive in TCP options (#1544).
