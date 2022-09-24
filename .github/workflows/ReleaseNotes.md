* [Core] MQTT Packets being sent over web socket transport are now setting the web socket frame boundaries correctly (#1499).
* [Core] Add support for attaching and detaching events from different threads.
* [Client] Keep alive mechanism now uses the configured timeout value from the options (thanks to @Stannieman, #1495).
* [Client] The _PingAsync_ will fallback to the timeout specified in the client options when the cancellation token cannot be cancelled.
* [Client] MQTTv5 features are now checked and an exception is thrown if they are used when using protocol version 3.1.1 and lower. These checks can be disabled in client options. (BREAKING CHANGE!)
* [Server] A DISCONNECT packet is no longer sent to MQTT clients < 5.0.0 (thanks to @logicaloud, #1506).
* [Server] Improved "take over" process handling.
