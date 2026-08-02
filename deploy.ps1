<#
.SYNOPSIS
    Builds a portable, dependency-free deploy package of LaserGRBL.

.DESCRIPTION
    LaserGRBL references only .NET Framework assemblies: every third party library it uses
    (SharpGL, CsPotrace, websocket-sharp, Clipper, ...) is compiled into the executable.
    This script builds the project and produces a folder (and optionally a zip) containing
    the executable and its data files only - no runtime DLL, no installer, no ClickOnce
    artifacts. It then verifies that nothing outside the .NET Framework is required.

    The only requirement left on the target machine is the .NET Framework 4.8 runtime,
    which ships with Windows 10 1903 and later.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER OutputRoot
    Where the package is created. Defaults to .\Deploy

.PARAMETER SkipBuild
    Package the existing build output instead of compiling again.

.PARAMETER NoZip
    Produce the folder only, without zipping it.

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -Configuration Debug -NoZip
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = (Join-Path $PSScriptRoot "Deploy"),
    [switch]$SkipBuild,
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"

$projectFile = Join-Path $PSScriptRoot "LaserGRBL\LaserGRBL.csproj"
$buildOutput = Join-Path $PSScriptRoot "LaserGRBL\bin\$Configuration"

# files that make up the portable package: the executable plus the data files it ships with
$packageFiles = @(
    "LaserGRBL.exe",
    "LaserGRBL.exe.config",
    "StandardButtons.zbn",
    "StandardMaterials.psh"
)

function Find-MSBuild
{
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path $vswhere)
    {
        $found = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" 2>$null | Select-Object -First 1
        if ($found) { return $found }
    }

    $onPath = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    # the project builds fine with the framework msbuild too
    $framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    if (Test-Path $framework) { return $framework }

    throw "MSBuild not found. Install Visual Studio or the Build Tools, or use -SkipBuild."
}

function Invoke-Build
{
    $msbuild = Find-MSBuild
    Write-Host "Building $Configuration with $msbuild" -ForegroundColor Cyan

    & $msbuild $projectFile /t:Rebuild /p:Configuration=$Configuration /p:Platform=AnyCPU /v:minimal /nologo /clp:ErrorsOnly

    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
}

# proves the package is self contained: every referenced assembly must come from the framework.
# ReflectionOnlyLoadFrom needs the .NET Framework, so it runs under Windows PowerShell 5.1.
function Test-NoExternalDependency([string]$exePath, [string]$packageDir)
{
    $strayFiles = Get-ChildItem $packageDir -File -Recurse -Include *.dll, *.so, *.dylib -ErrorAction SilentlyContinue

    if ($strayFiles)
    {
        $names = ($strayFiles | ForEach-Object { $_.Name }) -join ", "
        throw "The package contains external libraries: $names"
    }

    $winPS = Join-Path $env:WINDIR "System32\WindowsPowerShell\v1.0\powershell.exe"

    if (-not (Test-Path $winPS))
    {
        Write-Warning "Windows PowerShell not found: skipping the assembly reference check."
        return
    }

    $probe = Join-Path $env:TEMP "lasergrbl-refprobe.ps1"

    @'
param([string]$Path)
$asm = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($Path)
$asm.GetReferencedAssemblies() | ForEach-Object { $_.Name }
'@ | Set-Content -Path $probe -Encoding UTF8

    try
    {
        $references = & $winPS -NoProfile -ExecutionPolicy Bypass -File $probe -Path $exePath
    }
    finally
    {
        Remove-Item $probe -ErrorAction SilentlyContinue
    }

    # everything shipped with the .NET Framework itself
    $frameworkPrefixes = @("System", "mscorlib", "Microsoft.CSharp", "Microsoft.VisualBasic", "WindowsBase", "PresentationCore", "PresentationFramework", "Accessibility", "netstandard")
    $external = @()

    foreach ($reference in $references)
    {
        $isFramework = $false
        foreach ($prefix in $frameworkPrefixes)
        {
            if ($reference -eq $prefix -or $reference.StartsWith("$prefix.")) { $isFramework = $true; break }
        }
        if (-not $isFramework) { $external += $reference }
    }

    if ($external.Count -gt 0)
    {
        throw "The executable references non framework assemblies: $($external -join ', ')"
    }

    Write-Host "Dependency check: $($references.Count) references, all from the .NET Framework." -ForegroundColor Green
}

if (-not $SkipBuild) { Invoke-Build }

$exeFile = Join-Path $buildOutput "LaserGRBL.exe"
if (-not (Test-Path $exeFile)) { throw "Build output not found: $exeFile" }

$version = (Get-Item $exeFile).VersionInfo.FileVersion
$packageName = "LaserGRBL-$version-portable"
$packageDir = Join-Path $OutputRoot $packageName

if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

foreach ($name in $packageFiles)
{
    $source = Join-Path $buildOutput $name

    if (Test-Path $source)
    {
        Copy-Item $source -Destination $packageDir
    }
    else
    {
        Write-Warning "Not found in build output, skipped: $name"
    }
}

Test-NoExternalDependency -exePath (Join-Path $packageDir "LaserGRBL.exe") -packageDir $packageDir

$zipPath = "$packageDir.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (-not $NoZip) { Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath }

$totalSize = (Get-ChildItem $packageDir -File | Measure-Object -Property Length -Sum).Sum

Write-Host ""
Write-Host "Package: $packageDir" -ForegroundColor Cyan
Get-ChildItem $packageDir -File | ForEach-Object { "  {0,-26} {1,8:N0} KB" -f $_.Name, ($_.Length / 1KB) }
Write-Host ("  {0,-26} {1,8:N0} KB" -f "total", ($totalSize / 1KB))
if (-not $NoZip) { Write-Host "Zip:     $zipPath" -ForegroundColor Cyan }
Write-Host ""
Write-Host "Requires only .NET Framework 4.8 (bundled with Windows 10 1903 and later)." -ForegroundColor Green
