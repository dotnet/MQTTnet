param([string]$apiKey)

$files = Get-ChildItem -Path ".\NuGet" -Filter "*.nupkg"
foreach ($file in $files)
{
	Write-Host "Uploading: " $file

	.\nuget.exe push $file.Fullname $apiKey -NoSymbols -Source https://api.nuget.org/v3/index.json
}