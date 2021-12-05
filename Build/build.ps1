param([string]$assemblyVersion, [string]$nugetVersion)

if ([string]::IsNullOrEmpty($assemblyVersion)) {$assemblyVersion = "0.0.1"}
if ([string]::IsNullOrEmpty($nugetVersion)) {$nugetVersion = "0.0.1"}

$vswhere = ${Env:\ProgramFiles(x86)} + '\Microsoft Visual Studio\Installer\vswhere'
$msbuild = &$vswhere -products * -requires Microsoft.Component.MSBuild -latest -find MSBuild\**\Bin\MSBuild.exe

Write-Host
Write-Host "Assembly version = $assemblyVersion"
Write-Host "Nuget version    = $nugetVersion"
Write-Host "MSBuild path     = $msbuild"
Write-Host

# Cleanup
Get-ChildItem -Path ".\..\" -Filter "*.nupkg" -Recurse | Remove-Item
Get-ChildItem -Path ".\..\" -Filter "*.snupkg" -Recurse | Remove-Item

# Build and execute tests
&$msbuild ..\Tests\MQTTnet.Core.Tests\MQTTnet.Tests.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:TargetFramework="net5.0" /verbosity:m
&$msbuild ..\Tests\MQTTnet.AspNetCore.Tests\MQTTnet.AspNetCore.Tests.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:TargetFramework="net5.0" /verbosity:m

vstest.console.exe ..\Tests\MQTTnet.Core.Tests\bin\Release\net5.0\MQTTnet.Tests.dll
vstest.console.exe ..\Tests\MQTTnet.AspNetCore.Tests\bin\Release\net5.0\MQTTnet.AspNetCore.Tests.dll

$certificate = ".\..\..\Build\codeSigningKey.pfx"

# Build the core library
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /p:PackageVersion=$nugetVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$certificate

# Build the ASP.NET extension
&$msbuild ..\Source\MQTTnet.AspNetCore\MQTTnet.AspNetCore.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /p:PackageVersion=$nugetVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$certificate

# Build the RPC extension
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /p:PackageVersion=$nugetVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$certificate

# Build the Managed Client extension
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /p:PackageVersion=$nugetVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$certificate

# Build the WebSocket4Net extension
&$msbuild ..\Source\MQTTnet.Extensions.WebSocket4Net\MQTTnet.Extensions.WebSocket4Net.csproj /t:Clean /t:Restore /t:Build /p:Configuration="Release" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /p:PackageVersion=$nugetVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$certificate