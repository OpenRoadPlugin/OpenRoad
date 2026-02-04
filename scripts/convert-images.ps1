# Script de conversion des images pour Inno Setup
# Convertit OpenRoad_Logo.png en formats BMP et ICO

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$logoPath = Join-Path $root "OpenRoad_Logo.png"
$installerPath = Join-Path $root "installer"

Write-Host "Conversion des images pour l'installeur..." -ForegroundColor Cyan

if (-not (Test-Path $logoPath)) {
    Write-Error "Logo introuvable: $logoPath"
    exit 1
}

# Charger l'image source
$sourceImage = [System.Drawing.Image]::FromFile($logoPath)
Write-Host "  Source: $logoPath ($($sourceImage.Width)x$($sourceImage.Height))" -ForegroundColor Gray

# === 1. Créer WizardImage.bmp (164x314) ===
$wizardImage = New-Object System.Drawing.Bitmap(164, 314)
$graphics = [System.Drawing.Graphics]::FromImage($wizardImage)
$graphics.Clear([System.Drawing.Color]::White)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

# Centrer le logo dans la zone (garder aspect ratio)
$logoSize = 130
$x = [int]((164 - $logoSize) / 2)
$y = [int]((314 - $logoSize) / 2) - 50
$graphics.DrawImage($sourceImage, $x, $y, $logoSize, $logoSize)

# Ajouter texte "Open Road"
$font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(50, 50, 50))
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$graphics.DrawString("Open Road", $font, $brush, 82, 240, $sf)

$font2 = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 100, 100))
$graphics.DrawString("Plugin AutoCAD", $font2, $brush2, 82, 265, $sf)

$wizardImagePath = Join-Path $installerPath "WizardImage.bmp"
$wizardImage.Save($wizardImagePath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$graphics.Dispose()
$wizardImage.Dispose()
Write-Host "  WizardImage.bmp (164x314)" -ForegroundColor Green

# === 2. Créer WizardSmallImage.bmp (55x58) ===
$smallImage = New-Object System.Drawing.Bitmap(55, 58)
$graphics = [System.Drawing.Graphics]::FromImage($smallImage)
$graphics.Clear([System.Drawing.Color]::White)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.DrawImage($sourceImage, 3, 5, 49, 49)

$smallImagePath = Join-Path $installerPath "WizardSmallImage.bmp"
$smallImage.Save($smallImagePath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$graphics.Dispose()
$smallImage.Dispose()
Write-Host "  WizardSmallImage.bmp (55x58)" -ForegroundColor Green

# === 3. Créer OpenRoad_Logo.ico (multi-résolution) ===
# Note: System.Drawing ne crée pas de vrais ICO multi-résolution, on crée un 256x256
$icoImage = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($icoImage)
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.DrawImage($sourceImage, 0, 0, 256, 256)

# Convertir en icône
$icoPath = Join-Path $root "OpenRoad_Logo.ico"
$icon = [System.Drawing.Icon]::FromHandle($icoImage.GetHicon())
$fs = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
$graphics.Dispose()
$icoImage.Dispose()
Write-Host "  OpenRoad_Logo.ico (256x256)" -ForegroundColor Green

$sourceImage.Dispose()

Write-Host "`nConversion terminee!" -ForegroundColor Cyan
Write-Host "Fichiers crees:" -ForegroundColor Yellow
Write-Host "  - $wizardImagePath"
Write-Host "  - $smallImagePath"
Write-Host "  - $icoPath"
