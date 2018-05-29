# Import codeSigningKey.pfx
In order to import the key for code signing on a new developer machine use the following command within the VS developer command line:

> sn –i codeSigningKey.pfx VS_KEY_EFCA4C5B6DFD4B4F

The container name may be different.

# Check if the assembly has a strong name
> sn -vf MQTTnet.dll