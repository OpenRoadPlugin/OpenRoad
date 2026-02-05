[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'src\OAS.sln'
$installer = Join-Path $root 'installer\OAS.iss'
$versionFile = Join-Path $root 'version.json'
$inno = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'

Write-Host "Open Asphalte: build + installer" -ForegroundColor Cyan

# Extraire la version depuis version.json (source unique de v�rit�)
$versionJson = Get-Content $versionFile -Raw | ConvertFrom-Json
$version = $versionJson.version
Write-Host "Version: $version (from version.json)" -ForegroundColor Yellow

if (-not $SkipBuild) {
    Write-Host "Building Release..." -ForegroundColor Cyan
    dotnet build -c Release $solution
}

if (-not (Test-Path $inno)) {
    throw "Inno Setup introuvable: $inno"
}

Write-Host "Building installer..." -ForegroundColor Cyan
# Passer la version � Inno Setup via /D (d�finition de macro)
& $inno "/DMyAppVersion=$version" $installer

Write-Host "Done." -ForegroundColor Green
