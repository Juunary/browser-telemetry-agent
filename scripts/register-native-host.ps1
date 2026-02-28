param(
    [Parameter(Mandatory=$true)]
    [string]$ExtensionId
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

# Build the native host
Write-Host "Building native host..." -ForegroundColor Cyan
Push-Location "$RepoRoot\agent"
dotnet build src\Dlp.NativeHost --configuration Release
Pop-Location

# Determine the exe path
$ExePath = Join-Path $RepoRoot "agent\src\Dlp.NativeHost\bin\Release\net8.0\Dlp.NativeHost.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error "Native host exe not found at: $ExePath"
    exit 1
}

# Create the manifest
$ManifestName = "com.browser_telemetry.agent"
$Manifest = @{
    name = $ManifestName
    description = "Browser Telemetry DLP Native Messaging Host"
    path = $ExePath
    type = "stdio"
    allowed_origins = @("chrome-extension://$ExtensionId/")
} | ConvertTo-Json -Depth 4

$ManifestDir = Join-Path $RepoRoot "agent"
$ManifestPath = Join-Path $ManifestDir "native-host-manifest.json"
$Manifest | Out-File -FilePath $ManifestPath -Encoding UTF8 -Force

Write-Host "Manifest written to: $ManifestPath" -ForegroundColor Green

# Register in Windows Registry (HKCU for current user)
$RegPath = "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$ManifestName"
New-Item -Path $RegPath -Force | Out-Null
Set-ItemProperty -Path $RegPath -Name "(Default)" -Value $ManifestPath

Write-Host "Registry key created: $RegPath" -ForegroundColor Green
Write-Host ""
Write-Host "=== Setup complete ===" -ForegroundColor Green
Write-Host "Extension ID: $ExtensionId"
Write-Host "Manifest: $ManifestPath"
Write-Host "Host exe: $ExePath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Load the unpacked extension from: $RepoRoot\extension"
Write-Host "2. Open test-pages/clipboard.html"
Write-Host "3. Paste text with credit card number to see a warn banner"
