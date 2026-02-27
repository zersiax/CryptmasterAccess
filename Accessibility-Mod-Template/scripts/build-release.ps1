# Build and package CryptmasterAccess for release
# Usage: powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Cryptmaster"
$ReleaseName = "CryptmasterAccess-v$Version"
$ReleaseDir = Join-Path $ProjectRoot "release"
$StagingDir = Join-Path $ReleaseDir $ReleaseName
$ZipPath = Join-Path $ReleaseDir "$ReleaseName.zip"

Write-Host "Building CryptmasterAccess v$Version..."

# Build
Push-Location $ProjectRoot
dotnet build CryptmasterAccess.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# Clean staging
if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

# Create staging directory
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $StagingDir "Mods") -Force | Out-Null

# Copy mod DLL
$DllSource = Join-Path $ProjectRoot "bin\Release\net472\CryptmasterAccess.dll"
if (-not (Test-Path $DllSource)) {
    # Fall back to Debug build
    $DllSource = Join-Path $ProjectRoot "bin\Debug\net472\CryptmasterAccess.dll"
}
Copy-Item $DllSource (Join-Path $StagingDir "Mods\CryptmasterAccess.dll")

# Copy Tolk DLLs from game directory
Copy-Item (Join-Path $GameDir "Tolk.dll") $StagingDir
Copy-Item (Join-Path $GameDir "nvdaControllerClient64.dll") $StagingDir

# Copy README, changelog, and license
Copy-Item (Join-Path $ReleaseDir "README.txt") $StagingDir
Copy-Item (Join-Path $ProjectRoot "CHANGELOG.md") $StagingDir
Copy-Item (Join-Path $ProjectRoot "LICENSE") $StagingDir

Write-Host "Staging directory contents:"
Get-ChildItem $StagingDir -Recurse | ForEach-Object {
    $rel = $_.FullName.Substring($StagingDir.Length + 1)
    Write-Host "  $rel"
}

# Create ZIP
Compress-Archive -Path "$StagingDir\*" -DestinationPath $ZipPath -Force

Write-Host ""
Write-Host "Release package created: $ZipPath" -ForegroundColor Green
Write-Host "Size: $([math]::Round((Get-Item $ZipPath).Length / 1KB, 1)) KB"
Write-Host ""
Write-Host "To publish on GitHub:"
Write-Host "  gh release create v$Version `"$ZipPath`" --title `"v$Version - Initial Release`" --notes-file CHANGELOG.md"
