* [Core] Fixed a memory leak in _AsyncSignal_ implementation (#1586, thanks to @mario-zelger).
* [Core] Fixed an issue with bounds handling in _MqttBufferReader_ (#1593).
* [Core] Performance improvements and reduced memory usage due to improved buffer copies (#1599, thanks to @xljiulang).
* [Server] Fix not properly reset statistics (#1587, thanks to @damikun).
* [Server] Now using an empty string as the sender client ID for injected application messages (#1583, thanks to @xljiulang).
* [Server] Improved memory usage for ASP.NET connections (#1582, thanks to @xljiulang).
* [Server] Improved memory usage and performance for ASP.NET integration (#1596, thanks to @xljiulang).
* [Server] Add support for interception of will messages (#1613).
* [Server] Fixed wrong topic filter detections (#1615, thanks to @logicaloud).