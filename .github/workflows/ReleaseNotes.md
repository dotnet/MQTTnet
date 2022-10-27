* [Core] Fixed dead lock and race conditions in new _AsyncLock_ implementation (#1542).
* [Client] Fixed connection freeze when using Xamarin etc.
* [Client] MQTTv5 features are now checked and an exception is thrown if they are used when using protocol version 3.1.1 and lower. These checks can be disabled in client options. (BREAKING CHANGE!)
