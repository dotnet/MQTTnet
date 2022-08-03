* [Core] Fixed not working removal of event handlers from async events (#1479).
* [Client] Added support for passing the local endpoint (network card) which should be used (#1311).
* [Client] Exposed _PackageIdentifier_ in _MqttApplicationMessageReceivedEventArgs_ (thanks to @koepalex, #1466).
* [Server] Added a new event (_ClientAcknowledgedPublishPacketAsync_) which is fired whenever a client acknowledges a PUBLISH packet (QoS 1 and 2, #487).
* [Server] Exposed channel adapter (HTTP context etc.) in connection validation (#1125).
* [Server] The event _InterceptingPublishAsync_ is now also called for injected application messages (#Jeanot-Zubler).
* [Server] Exposed session items in multipe events (#1291).
