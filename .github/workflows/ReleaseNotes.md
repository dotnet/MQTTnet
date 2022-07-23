* [Core] Updated nuget packages.
* [Core] The option _IgnoreCertificateChainErrors_ is now respected (thanks to @GodVenn, #1447).
* [ManagedClient] The managed client now sends the entire topic filter including new MQTTv5 properties when subscribing.
* [ManagedClient] Fixed wrong firing of _ApplicationMessageSkippedAsync_ event (thanks to @quackgizmo, #1460).
* [Server] Fixed reporting of _MaximumQoS_ in _ConnAck_ packet (MQTTv5 only) (#1442).
* [Server] Fix cross thread issue in session message storage for QoS 1 and 2.
* [Server] The event _ClientSubscribedTopicAsync_ is now fired after the subscription is completely processed internally (#1435).
* [Server] Improved CPU usage on lower end machines (#788).
* [Client] Added support for passing the local endpoint which should be used (network card).