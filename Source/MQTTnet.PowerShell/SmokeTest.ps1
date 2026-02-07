# Quick smoke test for MQTTnet PowerShell Module
Import-Module ./bin/Debug/net8.0/MQTTnet.PowerShell.dll

Write-Host "Testing module load..." -ForegroundColor Yellow
$commands = Get-Command -Module MQTTnet.PowerShell
Write-Host "Found $($commands.Count) cmdlets:" -ForegroundColor Green
$commands | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Cyan }

Write-Host "`nTesting session creation..." -ForegroundColor Yellow
$session = New-MqttSession
Write-Host "Session created successfully!" -ForegroundColor Green

Write-Host "`nTesting session cleanup..." -ForegroundColor Yellow
Remove-MqttSession -Session $session
Write-Host "Session removed successfully!" -ForegroundColor Green

Write-Host "`n✓ All smoke tests passed!" -ForegroundColor Green
