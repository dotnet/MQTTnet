param([string]$version)

if ([string]::IsNullOrEmpty($version)) {$version = "0.0.1"}

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
&$msbuild ..\Frameworks\MQTTnet.NetFramework\MQTTnet.NetFramework.csproj /t:Build /p:Configuration="Release"
&$msbuild ..\Frameworks\MQTTnet.Netstandard\MQTTnet.Netstandard.csproj /t:Build /p:Configuration="Release"
&$msbuild ..\Frameworks\MQTTnet.UniversalWindows\MQTTnet.UniversalWindows.csproj /t:Build /p:Configuration="Release"



Remove-Item .\NuGet -Force -Recurse
New-Item -ItemType Directory -Force -Path .\NuGet
NuGet.exe pack MQTTnet.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $version