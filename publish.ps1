<#
.SYNOPSIS
    Build, pack, and deploy TylerBuilds Tracker via Velopack.

.DESCRIPTION
    Publishes the WPF app, packages it with Velopack, and deploys to the
    network update feed on Carl. The version number is injected into the
    csproj dynamically so there's no manual file editing.

.PARAMETER Version
    Semver2 version string (e.g. 1.0.0, 1.2.0-beta.1)

.EXAMPLE
    .\publish.ps1 -Version 1.0.1
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

# --- Paths ---
$ProjectDir   = "$PSScriptRoot\JobTrackerFrontend"
$CsprojPath   = "$ProjectDir\JobTrackerFrontend.csproj"
$PublishDir    = "$PSScriptRoot\publish"
$ReleasesDir  = "$PSScriptRoot\Releases"
$UpdateFeed   = "W:\Updates\TBJobTracker"
$ProductionDir = "W:\Production\TBJobTracker"

# --- Config ---
$PackId    = "TBJobTracker"
$PackTitle = "TylerBuilds JobTracker"
$MainExe   = "JobTrackerFrontend.exe"
$IconPath  = "$ProjectDir\assets\TBTrackerIcon_128.ico"

# ============================================================
# Step 0: Validate
# ============================================================
Write-Host "`n=== TylerBuilds Tracker — Publish v$Version ===" -ForegroundColor Cyan

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host "vpk CLI not found. Install with: dotnet tool install -g vpk" -ForegroundColor Red
    exit 1
}

# Ensure output directories exist
foreach ($dir in @($UpdateFeed, $ProductionDir, $ReleasesDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor DarkGray
    }
}

# ============================================================
# Step 1: Inject version into csproj
# ============================================================
Write-Host "`n[1/5] Setting version to $Version in csproj..." -ForegroundColor Yellow

$csproj = Get-Content $CsprojPath -Raw
$csproj = $csproj -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
Set-Content -Path $CsprojPath -Value $csproj -NoNewline
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# Step 2: dotnet publish
# ============================================================
Write-Host "`n[2/5] Publishing .NET app..." -ForegroundColor Yellow

if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

dotnet publish $CsprojPath `
    --self-contained `
    -c Release `
    -r win-x64 `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Published to $PublishDir" -ForegroundColor Green

# ============================================================
# Step 3: Download previous release (for delta generation)
# ============================================================
Write-Host "`n[3/5] Downloading previous release for delta..." -ForegroundColor Yellow

vpk download local --path $UpdateFeed -o $ReleasesDir 2>$null
Write-Host "  Done (no previous release is fine for first build)." -ForegroundColor Green

# ============================================================
# Step 4: vpk pack
# ============================================================
Write-Host "`n[4/5] Packing with Velopack..." -ForegroundColor Yellow

vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --packDir $PublishDir `
    --mainExe $MainExe `
    --packTitle $PackTitle `
    --icon $IconPath `
    --outputDir $ReleasesDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "vpk pack failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Packed successfully." -ForegroundColor Green

# ============================================================
# Step 5: Deploy to network share
# ============================================================
Write-Host "`n[5/5] Deploying to network share..." -ForegroundColor Yellow

# Upload to update feed (manages releases.json + retention)
vpk upload local `
    --outputDir $ReleasesDir `
    --path $UpdateFeed `
    --keepMaxReleases 5

if ($LASTEXITCODE -ne 0) {
    Write-Host "vpk upload failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Update feed deployed to $UpdateFeed" -ForegroundColor Green

# Copy installer + portable to production folder
$setupExe = Get-ChildItem "$ReleasesDir\*-Setup.exe" | Select-Object -First 1
$portable = Get-ChildItem "$ReleasesDir\*-Portable.zip" | Select-Object -First 1

if ($setupExe) {
    Copy-Item $setupExe.FullName "$ProductionDir\$($setupExe.Name)" -Force
    Write-Host "  Setup: $ProductionDir\$($setupExe.Name)" -ForegroundColor Green
}
if ($portable) {
    Copy-Item $portable.FullName "$ProductionDir\$($portable.Name)" -Force
    Write-Host "  Portable: $ProductionDir\$($portable.Name)" -ForegroundColor Green
}

# ============================================================
# Done
# ============================================================
Write-Host "`n=== v$Version deployed successfully! ===" -ForegroundColor Cyan
Write-Host "  Update feed:  $UpdateFeed"
Write-Host "  Installer:    $ProductionDir"
Write-Host ""
