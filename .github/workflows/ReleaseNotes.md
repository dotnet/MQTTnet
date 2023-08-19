* [Client] Fixed _PlatformNotSupportedException_ when using Blazor (#1755, thanks to @Nickztar).
* [Client] Added hot reload of client certificates (#1781).
* [Client] Added several new option builders and aligned usage (#1781, BREAKING CHANGE!).
* [Client] Fixed wrong logging of obsolete feature when connection was not successful (#1801, thanks to @ramonsmits).
* [Client] Fixed _NullReferenceException_ when performing several actions when not connected (#1800, thanks to @ramonsmits).
* [Server] Fixed _NullReferenceException_ in retained messages management (#1762, thanks to @logicaloud).
* [Server] Exposed new option which allows disabling packet fragmentation (#1753).
* [Server] Expired sessions will no longer be used when a client connects (#1756).
* [Server] Fixed an issue in connection handling for ASP.NET connections (#1819, thanks to @CZEMacLeod).
