* [Client] Added support for passing MQTT v5 options (User properties etc.) for disconnects.
* [Client] An internal exception is no longer caught silently when calling _DisconnectAsync_ to indicate that the disconnect is not clean (BREAKING CHANGE).
* [Server] Exposed MQTT v5 sent properties from the affected client in _ClientDisconnectedAsync_ event.