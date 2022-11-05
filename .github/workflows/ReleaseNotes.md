* [Core] Fixed dead lock and race conditions in new _AsyncLock_ implementation (#1542).
* [Client] Fixed connection freeze when using Xamarin etc.
* [Client] All waiting tasks will now fail when the client connection gets closed. This fixes several freezes (#1561).