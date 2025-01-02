**! Read the migration guide (https://github.com/dotnet/MQTTnet/wiki/Upgrading-guide) before migrating to version 5!**

## Changes
* Server: Fixed dead lock when awaiting a packet transmission but the packet gets dropped due to quotas (#2117, thanks to @AntonSmolkov)
