**! Read the migration guide (https://github.com/dotnet/MQTTnet/wiki/Upgrading-guide) before migrating to version 5!**

## Changes
* Memory usage optimizations (thanks to @mregen)
* Performance optimizations (thanks to @mregen)
* Removal of no longer supported .NET Frameworks **(BREAKING CHANGE)**
* Changed code signing and nuget certificate
* Namespace changes **(BREAKING CHANGE)**
* Removal of Managed Client **(BREAKING CHANGE)**
* Client: MQTT 5.0.0 is now the default version when connecting with a server **(BREAKING CHANGE)**
* Client: Exposed WebSocket compression options in MQTT client options (thanks to @victornor, #2127)
* Server: Set default for "MaxPendingMessagesPerClient" to 1000 **(BREAKING CHANGE)**
* Server: Set SSL version to "None" which will let the OS choose the version **(BREAKING CHANGE)**
* Server: Added API for getting a single session (thanks to @AntonSmolkov, #2131)
* Server: Fixed "TryPrivate" (Mosquitto feature) handling (thanks to @victornor, #2125) **(BREAKING CHANGE)**
