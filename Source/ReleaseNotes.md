**! Read the migration guide (https://github.com/dotnet/MQTTnet/wiki/Upgrading-guide) before migrating to version 5!**

## Changes
* Memory usage optimizations (thanks to @mregen)
* Performance optimizations (thanks to @mregen)
* Removal of no longer supported .NET Frameworks **(BREAKING CHANGE)**
* Changed code signing and nuget certificate
* Namespace changes **(BREAKING CHANGE)**
* Removal of Managed Client **(BREAKING CHANGE)**
* Fixed missing release notes in nuget packages.


* Server: Set default for "MaxPendingMessagesPerClient" to 1000 **(BREAKING CHANGE)**
* Server: Set SSL version to "None" which will let the OS choose the version **(BREAKING CHANGE)**
* Client: Fixed enhanced authentication.
* Client: Exposed WebSocket compression options in MQTT client options (thanks to @victornor, #2127)
* Client: Fixed wrong timeout for keep alive check (thanks to @Erw1nT, #2129)
* Server: Set default for "MaxPendingMessagesPerClient" to 1000 **(BREAKING CHANGE)**
* Server: Set SSL version to "None" which will let the OS choose the version **(BREAKING CHANGE)**
* Server: Fixed enhanced authentication.
* Server: Set default for "MaxPendingMessagesPerClient" to 1000 **(BREAKING CHANGE)**
* Server: Set SSL version to "None" which will let the OS choose the version **(BREAKING CHANGE)**
* Server: Added API for getting a single session (thanks to @AntonSmolkov, #2131)
* Server: Fixed "TryPrivate" (Mosquitto feature) handling (thanks to @victornor, #2125) **(BREAKING CHANGE)**
* Server: Fixed dead lock when awaiting a packet transmission but the packet gets dropped due to quotas (#2117, thanks to @AntonSmolkov)
