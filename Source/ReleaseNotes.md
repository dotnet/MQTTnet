**! Read the migration guide (https://github.com/dotnet/MQTTnet/wiki/Upgrading-guide) before migrating to version 5!**

## Changes
* Memory usage optimizations (thanks to @mregen)
* Performance optimizations (thanks to @mregen)
* Removal of no longer supported .NET Frameworks **(BREAKING CHANGE)**
* Changed code signing and nuget certificate
* Namespace changes **(BREAKING CHANGE)**
* Removal of Managed Client **(BREAKING CHANGE)**
* Client: MQTT 5.0.0 is now the default version when connecting with a server **(BREAKING CHANGE)**
* Client: TryPrivate is no longer enabled by default **(BREAKING CHANGE)**
* Server: Set default for "MaxPendingMessagesPerClient" to 1000 **(BREAKING CHANGE)**
* Server: Set SSL version to "None" which will let the OS choose the version **(BREAKING CHANGE)**
* Server: Fixed dead lock when awaiting a packet transmission but the packet gets dropped due to quotas (#2117, thanks to @AntonSmolkov)