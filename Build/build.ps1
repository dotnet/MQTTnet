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

# Build the core library
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net452" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net461" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard1.3" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard2.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet\MQTTnet.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="uap10.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"

# Build the ASP.NET Core 2.0 extension
&$msbuild ..\Source\MQTTnet.AspNetCore\MQTTnet.AspNetCore.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard2.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"

# Build the RPC extension
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net452" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net461" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard1.3" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard2.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.Rpc\MQTTnet.Extensions.Rpc.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="uap10.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"

# Build the Managed Client extension
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net452" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net461" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard1.3" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard2.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"
&$msbuild ..\Source\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="uap10.0" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"

# Build MQTTnet.Server
Remove-Item ..\Source\MQTTnet.Server\bin\Release\netcoreapp2.2 -Recurse -Force -ErrorAction SilentlyContinue
&$msbuild ..\Source\MQTTnet.Server\MQTTnet.Server.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netcoreapp2.2" /p:FileVersion=$assemblyVersion /p:AssemblyVersion=$assemblyVersion /verbosity:m /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=".\..\..\Build\codeSigningKey.pfx"

$source = "..\Source\MQTTnet.Server\bin\Release\netcoreapp2.2"
$destination = "..\Source\MQTTnet.Server\bin\MQTTnet.Server-Portable-v$nugetVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 

# Create NuGet packages.
Remove-Item .\NuGet -Force -Recurse -ErrorAction SilentlyContinue

Copy-Item MQTTnet.AspNetCore.nuspec -Destination MQTTnet.AspNetCore.nuspec.old -Force
(Get-Content MQTTnet.AspNetCore.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.AspNetCore.nuspec
Copy-Item MQTTnet.Extensions.Rpc.nuspec -Destination MQTTnet.Extensions.Rpc.nuspec.old -Force
(Get-Content MQTTnet.Extensions.Rpc.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.Extensions.Rpc.nuspec
Copy-Item MQTTnet.Extensions.ManagedClient.nuspec -Destination MQTTnet.Extensions.ManagedClient.nuspec.old -Force
(Get-Content MQTTnet.Extensions.ManagedClient.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.Extensions.ManagedClient.nuspec

New-Item -ItemType Directory -Force -Path .\NuGet
.\nuget.exe pack MQTTnet.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
.\nuget.exe pack MQTTnet.NETStandard.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
.\nuget.exe pack MQTTnet.AspNetCore.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
.\nuget.exe pack MQTTnet.Extensions.Rpc.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
.\nuget.exe pack MQTTnet.Extensions.ManagedClient.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion

Move-Item MQTTnet.AspNetCore.nuspec.old -Destination MQTTnet.AspNetCore.nuspec -Force
Move-Item MQTTnet.Extensions.Rpc.nuspec.old -Destination MQTTnet.Extensions.Rpc.nuspec -Force
Move-Item MQTTnet.Extensions.ManagedClient.nuspec.old -Destination MQTTnet.Extensions.ManagedClient.nuspec -Force