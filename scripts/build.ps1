$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "=== Building Extension ===" -ForegroundColor Cyan
Push-Location "$RepoRoot\extension"
if (Test-Path "package.json") {
    npm ci
    npm run build
} else {
    Write-Host "  (no package.json yet - skipping)"
}
Pop-Location

Write-Host ""
Write-Host "=== Building .NET Agent ===" -ForegroundColor Cyan
Push-Location "$RepoRoot\agent"
$slnFiles = Get-ChildItem -Filter "*.sln" -ErrorAction SilentlyContinue
if ($slnFiles) {
    dotnet build --configuration Release
    dotnet test --configuration Release --no-build
} else {
    Write-Host "  (no .sln yet - skipping)"
}
Pop-Location

Write-Host ""
Write-Host "=== Build complete ===" -ForegroundColor Green
