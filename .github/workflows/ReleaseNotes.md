* [Server] Connections with _try_private_ flag (MQTT 3.1.1 Bridge) are now accepted (#1413).
* [Client] Added the flag _try_private_ (MQTT 3.1.1 Bridge) to client options (#1413).
* [Client] Fixed MQTTv5 protocol violation in PUBLISH packets (#1423).
* [Client] Added missing "WithWill..." methods in _MqttClientOptionsBuilder_.
* [ManagedClient] Fixed wrong event args type for connected and disconnected events (#1432).