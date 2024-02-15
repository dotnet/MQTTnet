* [Client] Exposed the _EndPoint_ type to support other endpoint types (like Unix Domain Sockets) in client options (#1919).
* [ManagedClient] Added support for intercepting (and cancelling) publish messages (#1915, thanks to @hannasm).
* [Server] Added new events for delivered and dropped messages (#1866, thanks to @kallayj).
* [Server] The server will no longer treat a client which is receiving a large payload as alive. The packet must be received completely within the keep alive boundaries (BREAKING CHANGE!, #1883).
* [Server] Fixed "service not registered" exception in ASP.NET integration (#1889).
