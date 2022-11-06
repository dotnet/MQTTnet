* [Core] Fixed several dead locks by implementing a new async signal (#1552)
* [Server] Fixed duplicated invocation of the event _ClientAcknowledgedPublishPacketAsync_ for QoS level 2 (#1550)
* [Server] Fixed issue in upgrading and downgrading of QoS levels for subscriptions and retained messages (#1560)