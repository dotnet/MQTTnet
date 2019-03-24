param([string]$assemblyVersion, [string]$nugetVersion)

if ([string]::IsNullOrEmpty($assemblyVersion)) {$assemblyVersion = "0.0.1"}
if ([string]::IsNullOrEmpty($nugetVersion)) {$nugetVersion = "0.0.1"}

Write-Host "Assembly version = $assemblyVersion"
Write-Host "Nuget version    = $nugetVersion"
Write-Host

$vswhere = ${Env:\ProgramFiles(x86)} + '\Microsoft Visual Studio\Installer\vswhere'

$path = &$vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($path) {
    $msbuild = join-path $path 'MSBuild\15.0\Bin\MSBuild.exe'

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

	Remove-Item .\NuGet -Force -Recurse -ErrorAction SilentlyContinue

    Copy-Item MQTTnet.AspNetCore.nuspec -Destination MQTTnet.AspNetCore.nuspec.old
	(Get-Content MQTTnet.AspNetCore.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.AspNetCore.nuspec
    Copy-Item MQTTnet.Extensions.Rpc.nuspec -Destination MQTTnet.Extensions.Rpc.nuspec.old
    (Get-Content MQTTnet.Extensions.Rpc.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.Extensions.Rpc.nuspec
    Copy-Item MQTTnet.Extensions.ManagedClient.nuspec -Destination MQTTnet.Extensions.ManagedClient.nuspec.old
    (Get-Content MQTTnet.Extensions.ManagedClient.nuspec) -replace '\$nugetVersion', $nugetVersion | Set-Content MQTTnet.Extensions.ManagedClient.nuspec
	
	New-Item -ItemType Directory -Force -Path .\NuGet
    .\nuget.exe pack MQTTnet.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
    .\nuget.exe pack MQTTnet.NETStandard.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
    .\nuget.exe pack MQTTnet.AspNetCore.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
	.\nuget.exe pack MQTTnet.Extensions.Rpc.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
    .\nuget.exe pack MQTTnet.Extensions.ManagedClient.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $nugetVersion
    
    Copy-Item MQTTnet.AspNetCore.nuspec.old -Destination MQTTnet.AspNetCore.nuspec
    Copy-Item MQTTnet.Extensions.Rpc.nuspec.old -Destination MQTTnet.Extensions.Rpc.nuspec
    Copy-Item MQTTnet.Extensions.ManagedClient.nuspec.old -Destination MQTTnet.Extensions.ManagedClient.nuspec
}