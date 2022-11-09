* [Core] Fixed several dead locks by implementing a new async signal (#1552)
* [Client] All waiting tasks will now fail when the client connection gets closed. This fixes several freezes (#1561)
* [ManagedClient] The topic is no longer declared as invalid if a topic alias is used (#1395).
* [Server] Fixed duplicated invocation of the event _ClientAcknowledgedPublishPacketAsync_ for QoS level 2 (#1550)
* [Server] Fixed issue in upgrading and downgrading of QoS levels for subscriptions and retained messages (#1560)
* [Server] Fixed memory leak when old sessions are discarded (#1553)
