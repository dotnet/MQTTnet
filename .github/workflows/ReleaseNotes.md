* [Core] Fixed a memory leak in _AsyncSignal_ implementation (#1586, thanks to @mario-zelger).
* [Client] Added support for passing MQTT v5 options (User properties etc.) for disconnects.
* [Client] An internal exception is no longer caught silently when calling _DisconnectAsync_ to indicate that the disconnect is not clean (BREAKING CHANGE).
* [Server] Fix not properly reset statistics (#1587, thanks to @damikun).
* [Server] Now using an empty string as the sender client ID for injected application messages (#1583, thanks to @xljiulang).
* [Server] Exposed MQTT v5 sent properties from the affected client in _ClientDisconnectedAsync_ event.