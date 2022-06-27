* [Core] Updated nuget packages.
* [Server] Fixed reporting of _MaximumQoS_ in _ConnAck_ packet (MQTTv5 only) (#1442).
* [Server] Fix cross thread issue in session message storage for QoS 1 and 2.
* [Server] The event _ClientSubscribedTopicAsync_ is now fired after the subscription is completely processed internally (#1435).