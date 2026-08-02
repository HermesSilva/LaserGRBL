<#
.SYNOPSIS
    Builds a self extracting LaserGRBL installer using IExpress, shipped with Windows.

.DESCRIPTION
    Runs deploy.ps1 to produce the dependency free package, adds the installation scripts
    and the file type icons, and wraps everything in a single setup executable built with
    iexpress.exe (no external tool required).

    The generated setup installs for all users: it elevates through UAC, copies the
    application to Program Files, creates the start menu and desktop shortcuts, registers
    the .nc, .zbn and .lps file associations, and adds an entry to Add/Remove Programs.

.PARAMETER Configuration
    Build configuration used for the application. Defaults to Release.

.PARAMETER OutputRoot
    Where the setup executable is written. Defaults to .\Deploy

.PARAMETER SkipBuild
    Reuse the existing build output instead of compiling again.

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = (Join-Path $PSScriptRoot "Deploy"),
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
if (-not (Test-Path $iexpress)) { throw "iexpress.exe not found: this script requires Windows." }

# 1. build the portable package (also proves there is no external dependency)
$deployArgs = @{ Configuration = $Configuration; OutputRoot = $OutputRoot; NoZip = $true }
if ($SkipBuild) { $deployArgs.SkipBuild = $true }

& (Join-Path $PSScriptRoot "deploy.ps1") @deployArgs | Write-Host

$package = Get-ChildItem $OutputRoot -Directory -Filter "LaserGRBL-*-portable" |
           Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $package) { throw "Portable package not found under $OutputRoot" }

$version = (Get-Item (Join-Path $package.FullName "LaserGRBL.exe")).VersionInfo.FileVersion

# 2. stage payload: application files + installer scripts + file type icons
$stage = Join-Path $env:TEMP ("lasergrbl-setup-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $stage | Out-Null

try
{
    Copy-Item (Join-Path $package.FullName "*") -Destination $stage

    foreach ($icon in @("lasergrblfile.ico", "zippedbutton.ico"))
    {
        $iconPath = Join-Path $PSScriptRoot $icon
        if (Test-Path $iconPath) { Copy-Item $iconPath -Destination $stage } else { Write-Warning "Icon not found: $icon" }
    }

    # version is baked into the installer script
    $installScript = Get-Content (Join-Path $PSScriptRoot "installer\install.cmd") -Raw
    $installScript = $installScript.Replace("__VERSION__", $version)
    Set-Content -Path (Join-Path $stage "install.cmd") -Value $installScript -Encoding ASCII

    Copy-Item (Join-Path $PSScriptRoot "installer\uninstall.cmd") -Destination $stage

    # 3. write the IExpress directive file
    if (-not (Test-Path $OutputRoot)) { New-Item -ItemType Directory -Path $OutputRoot | Out-Null }

    $setupExe = Join-Path (Resolve-Path $OutputRoot) "LaserGRBL-$version-setup.exe"
    if (Test-Path $setupExe) { Remove-Item $setupExe -Force }

    $payload = Get-ChildItem $stage -File | Sort-Object Name
    $sedPath = Join-Path $stage "setup.sed"

    $fileStrings = @()
    $fileEntries = @()

    for ($i = 0; $i -lt $payload.Count; $i++)
    {
        $fileStrings += "FILE$i=`"$($payload[$i].Name)`""
        $fileEntries += "%FILE$i%="
    }

    $sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[Strings]
InstallPrompt=Install LaserGRBL $version on this computer?
DisplayLicense=
FinishMessage=
TargetName=$setupExe
FriendlyName=LaserGRBL $version Setup
AppLaunched=cmd.exe /c install.cmd
PostInstallCmd=<None>
$($fileStrings -join "`r`n")
[SourceFiles]
SourceFiles0=$stage
[SourceFiles0]
$($fileEntries -join "`r`n")
"@

    Set-Content -Path $sedPath -Value $sed -Encoding ASCII

    # 4. build it
    Write-Host "Building installer with IExpress..." -ForegroundColor Cyan
    & $iexpress /N /Q $sedPath | Out-Null

    if (-not (Test-Path $setupExe)) { throw "IExpress did not produce $setupExe" }

    $size = (Get-Item $setupExe).Length

    Write-Host ""
    Write-Host "Installer: $setupExe" -ForegroundColor Cyan
    Write-Host ("  {0:N0} KB, payload: {1} files" -f ($size / 1KB), $payload.Count)
    Write-Host ""
    Write-Host "Installs for all users (UAC), with shortcuts, file associations and uninstaller." -ForegroundColor Green
}
finally
{
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
