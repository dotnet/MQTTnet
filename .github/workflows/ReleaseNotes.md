* [Client] Added support for custom CA chain validation (#1851, thanks to @rido-min).
* [Client] Added support for custom CA chain validation (#1851, thanks to @rido-min).
* [Client] Fixed handling of unobserved tasks exceptions (#1871).
* [Client] Fixed not specified ReasonCode when using _SendExtendedAuthenticationExchangeDataAsync_ (#1882, thanks to @rido-min).
* [Server] Fixed not working _UpdateRetainedMessageAsync_ public api (#1858, thanks to @kimdiego2098).
* [Server] Added support for custom DISCONNECT packets when stopping the server or disconnect a client (BREAKING CHANGE!, #1846).
* [Server] Added new property to stop the server from accepting new connections even if it is running (#1846).
* [Server] Fixed "service not registered" exception in ASP.NET integration (#1889).