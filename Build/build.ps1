param([string]$version)

if ([string]::IsNullOrEmpty($version)) {$version = "0.0.1"}

$vswhere = ${Env:\ProgramFiles(x86)} + '\Microsoft Visual Studio\Installer\vswhere'

$path = &$vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($path) {
    $msbuild = join-path $path 'MSBuild\15.0\Bin\MSBuild.exe'

    &$msbuild ..\Frameworks\MQTTnet.Netstandard\MQTTnet.Netstandard.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="net451" /p:FileVersion=$version /p:AssemblyVersion=$version /verbosity:m
    &$msbuild ..\Frameworks\MQTTnet.Netstandard\MQTTnet.Netstandard.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard1.3" /p:FileVersion=$version /p:AssemblyVersion=$version /verbosity:m
    &$msbuild ..\Frameworks\MQTTnet.Netstandard\MQTTnet.Netstandard.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="uap10.0" /p:FileVersion=$version /p:AssemblyVersion=$version /verbosity:m
    &$msbuild ..\Frameworks\MQTTnet.AspNetCore\MQTTnet.AspNetCore.csproj /t:Build /p:Configuration="Release" /p:TargetFramework="netstandard2.0" /p:FileVersion=$version /p:AssemblyVersion=$version /verbosity:m

    Remove-Item .\NuGet -Force -Recurse
    New-Item -ItemType Directory -Force -Path .\NuGet
    .\NuGet.exe pack MQTTnet.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $version
    .\NuGet.exe pack MQTTnet.AspNetCore.nuspec -Verbosity detailed -Symbols -OutputDir "NuGet" -Version $version
}

