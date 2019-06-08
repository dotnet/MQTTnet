# Starting portable version
The portable version requires a local installation of the .net core runtime. With that runtime installed the server can be started via the following comand.

dotnet .\MQTTnet.Server.dll

# Starting self contained versions
The self contained versions are fully portable versions including the .net core runtime. The server can be started using the contained executable files.

Windows:    MQTTnet.Server.exe
Linux:		MQTTnet.Server (must be set to _executable_ first)
