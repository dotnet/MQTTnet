* [Client] Added support for passing the local endpoint which should be used (network card).
* [Client] Exposed _PackageIdentifier_ in _MqttApplicationMessageReceivedEventArgs_ (thanks to @koepalex, #1466).
* [Server] Added a new event (_ClientAcknowledgedPublishPacketAsync_) which is fired whenever a client acknowledges a PUBLISH packet (QoS 1 and 2, #487).
* [Server] Exposed channel adapter (HTTP context etc.) in connection validation (#1125).
