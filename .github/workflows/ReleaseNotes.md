* [Server] Added new events for delivered and dropped messages (#1866, thanks to @kallayj).
* [Server] The server will no longer treat a client which is receiving a large payload as alive. The packet must be received completely within the keep alive boundaries (BREAKING CHANGE!, #1883).
