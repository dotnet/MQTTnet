* [Client] Fixed _PlatformNotSupportedException_ when using Blazor (#1755, thanks to @Nickztar).
* [Client] Added support for _RemoteCertificateValidationCallback_ for .NET 4.5.2, 4.6.1 and 4.8 (#1806, thanks to @troky).
* [Server] Fixed _NullReferenceException_ in retained messages management (#1762, thanks to @logicaloud).
* [Server] Exposed new option which allows disabling packet fragmentation (#1753).
* [Server] Expired sessions will no longer be used when a client connects (#1756).
