# MQTT Scripts

This directory contains scripts which are loaded by the server and can be used to perform the following tasks.

* Validation of client connections via credentials, client IDs etc.
* Manipulation of every processed message.
* Validation of subscription attempts.
* Publishing of custom application messages.

All scripts are loaded and _MQTTnet Server_ will invoke functions according to predefined naming conventions.
If a function is implemented in multiple scripts the context will be moved throug all instances. This allows overriding of results or passing data to other (following) scripts.

The Python starndard library ships with _MQTTnet Server_. But it is possible to add custom paths with Python libraries.

```
import sys
sys.path.append(PATH_TO_LIBRARY)
```

* All scripts must have the file extension _.py_. 
* All scripts are sorted alphabetically (A to Z) before being loaded and parsed.