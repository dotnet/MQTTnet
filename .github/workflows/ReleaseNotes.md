* [Core] Add validation of maximum string lengths (#1718).
* [Core] Added .NET 4.8 builds (#1729).
* [Core] Exposed more details of DISCONNECT packet in log (#1729).
* [Client] Added overloads for setting packet payload and will payload (#1720).
* [Client] The proper connect result is now exposed in the _Disconnected_ event when authentication fails (#1139).
* [Client] Exposed _UseDefaultCredentials_ and more for Web Socket options and proxy options (#1734, thanks to @impworks).
* [Client] Exposed more TLS options (#1729).
* [Client] Fixed wrong return code conversion (#1729).
* [Server] Improved performance by changing internal locking strategy for subscriptions (#1716, thanks to @zeheng).
