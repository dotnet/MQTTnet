param([string]$apiKey)

Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile "nuget.exe"

$files = Get-ChildItem -Path ".\NuGet" -Filter "*.nupkg"
foreach ($file in $files)
{
	Write-Host "Uploading: " $file

	.\nuget.exe push $file.Fullname $apiKey -Source https://api.nuget.org/v3/index.json
}

Remove-Item "nuget.exe" -Force -Recurse -ErrorAction SilentlyContinue