param([string]$version)

if ([string]::IsNullOrEmpty($version)) {$version = "0.0.1"}

$msbuild = "MSBuild.exe"
&dotnet build ..\Frameworks\MQTTnet.Netstandard\MQTTnet.Netstandard.csproj -c="Release" /p:FileVersion=$version /p:AssemblyVersion=$version
&dotnet build ..\Frameworks\MQTTnet.AspNetCore\MQTTnet.AspNetCore.csproj -c="Release" /p:FileVersion=$version /p:AssemblyVersion=$version
&$msbuild ..\Frameworks\MQTTnet.UniversalWindows\MQTTnet.UniversalWindows.csproj /t:Build /p:Configuration="Release"

Remove-Item .\NuGet -Force -Recurse
New-Item -ItemType Directory -Force -Path .\NuGet
.\NuGet.exe pack MQTTnet.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $version
.\NuGet.exe pack MQTTnet.AspNetCore.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $version