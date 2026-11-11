<#
.SYNOPSIS
    Builds the clinic package of Ordinacija with the clinic connection settings, then verifies it.

.DESCRIPTION
    Uses the tracked publish profiles (Clinic-ClickOnce / Clinic-Folder). Those switch the package to
    Ordinacija\appsettings.Deploy.json (untracked, holds the clinic connection string) and run a guard
    in the .csproj that refuses missing, placeholder or dev-server settings.

    After publishing, this script opens the PACKAGED settings and checks them. It never prints the password.

    See Deploy\DEPLOY.md.

.PARAMETER Mode
    ClickOnce (default): setup.exe + versioned "Application Files" folder, built with Visual Studio's MSBuild.
    Folder: plain folder publish with dotnet publish.

.PARAMETER OutputDir
    Test publish into another folder. Nothing in C:\OrdinacijaDeploy is touched and the ClickOnce
    revision number in the profile is not changed.

.PARAMETER AllowDirty
    Publish even though there are uncommitted changes (the package then cannot be reproduced from git).

.EXAMPLE
    .\Deploy\Publish-Ordinacija.ps1
    .\Deploy\Publish-Ordinacija.ps1 -Mode Folder
    .\Deploy\Publish-Ordinacija.ps1 -OutputDir C:\Temp\ordinacija-test -AllowDirty
#>
[CmdletBinding()]
param(
    [ValidateSet('ClickOnce', 'Folder')]
    [string]$Mode = 'ClickOnce',
    [string]$OutputDir,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'

$repo        = Split-Path -Parent $PSScriptRoot
$projDir     = Join-Path $repo 'Ordinacija'
$csproj      = Join-Path $projDir 'Ordinacija.csproj'
$profileName = if ($Mode -eq 'ClickOnce') { 'Clinic-ClickOnce' } else { 'Clinic-Folder' }
$profilePath = Join-Path $projDir "Properties\PublishProfiles\$profileName.pubxml"
$deployFile  = Join-Path $projDir 'appsettings.Deploy.json'
$localFile   = Join-Path $projDir 'appsettings.Local.json'

function Get-DataSource([string]$connectionString) {
    [regex]::Match($connectionString, 'Data Source=[^;]*').Value
}

function Read-PantheonDb([string]$path) {
    $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $cs = $json.ConnectionStrings.PantheonDB
    if (-not $cs) { return $null }
    [pscustomobject]@{
        ConnectionString = $cs
        Server           = Get-DataSource $cs
        HasPassword      = [regex]::IsMatch($cs, 'Password=[^;]+')
    }
}

# ---------------------------------------------------------------- preconditions
if (-not (Test-Path -LiteralPath $profilePath)) { throw "Publish profile not found: $profilePath" }
if (-not (Test-Path -LiteralPath $deployFile)) {
    throw "Missing $deployFile`nCreate it with the clinic's ConnectionStrings:PantheonDB (see Deploy\DEPLOY.md)."
}

$commit = (git -C $repo rev-parse --short HEAD).Trim()
$branch = (git -C $repo branch --show-current).Trim()
$dirty  = @(git -C $repo status --porcelain)
if ($dirty.Count -gt 0 -and -not $AllowDirty) {
    throw "There are uncommitted changes. Commit them first so the package can be reproduced, or pass -AllowDirty.`n$($dirty -join "`n")"
}

$isTest = [bool]$OutputDir
if ($isTest) { $OutputDir = $OutputDir.TrimEnd('\') }

if ($OutputDir) {
    $outRoot = $OutputDir
} else {
    $m = [regex]::Match((Get-Content -LiteralPath $profilePath -Raw), '<PublishDir>(.*?)</PublishDir>')
    if (-not $m.Success) { throw "No <PublishDir> in $profilePath" }
    $outRoot = $m.Groups[1].Value.TrimEnd('\')
}

Write-Host "Mode:    $Mode ($profileName)"
Write-Host "Commit:  $commit on $branch$(if ($dirty.Count -gt 0) { ' (UNCOMMITTED CHANGES)' })"
Write-Host "Output:  $outRoot$(if ($isTest) { '  [test publish]' })"
Write-Host ''

# ---------------------------------------------------------------- publish
Push-Location $projDir
try {
    if ($Mode -eq 'ClickOnce') {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (-not (Test-Path -LiteralPath $vswhere)) { throw 'vswhere.exe not found. ClickOnce needs Visual Studio 2022 (its MSBuild builds setup.exe).' }
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if (-not $msbuild) { throw 'Visual Studio MSBuild not found.' }

        $profileText = Get-Content -LiteralPath $profilePath -Raw
        $rev = [int][regex]::Match($profileText, '<ApplicationRevision>(\d+)</ApplicationRevision>').Groups[1].Value
        $next = $rev + 1
        Write-Host "ClickOnce version: 1.0.0.$next"

        $msbuildArgs = @(
            $csproj, '/restore', '/t:Publish', '/p:Configuration=Release',
            "/p:PublishProfile=$profileName",
            '/p:IsRevisionIncremented=false', "/p:ApplicationRevision=$next",
            '/nologo', '/v:m', '/nr:false'
        )
        if ($OutputDir) { $msbuildArgs += "/p:PublishDir=$OutputDir", "/p:PublishUrl=$OutputDir" }

        & $msbuild @msbuildArgs
        if ($LASTEXITCODE -ne 0) { throw "MSBuild publish failed (exit code $LASTEXITCODE)." }

        if (-not $isTest) {
            # Record the new revision in the tracked profile so the next publish continues from it.
            $updated = $profileText -replace '<ApplicationRevision>\d+</ApplicationRevision>', "<ApplicationRevision>$next</ApplicationRevision>"
            [System.IO.File]::WriteAllText($profilePath, $updated, (New-Object System.Text.UTF8Encoding($false)))
        }
    } else {
        $dotnetArgs = @('publish', $csproj, '-c', 'Release', "-p:PublishProfile=$profileName", '-nologo', '-v:m')
        if ($OutputDir) { $dotnetArgs += "-p:PublishDir=$OutputDir" }
        & dotnet @dotnetArgs
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)." }
    }
} finally {
    Pop-Location
}

# ---------------------------------------------------------------- verify the package
Write-Host ''
Write-Host 'Checking the published package...'

if ($Mode -eq 'ClickOnce') {
    $versionDir = Get-ChildItem -LiteralPath (Join-Path $outRoot 'Application Files') -Directory -Filter 'Ordinacija_*' |
        Sort-Object { [version]($_.Name -replace '^Ordinacija_', '' -replace '_', '.') } | Select-Object -Last 1
    if (-not $versionDir) { throw "No Application Files\Ordinacija_* folder under $outRoot" }
    $packageDir  = $versionDir.FullName
    $packageName = $versionDir.Name
    $settingsFile = Join-Path $packageDir 'appsettings.Local.json.deploy'
    $baseFile     = Join-Path $packageDir 'appsettings.json.deploy'
    $leaked = @(Get-ChildItem -LiteralPath $packageDir -File -Filter 'appsettings.Deploy.json*')
    $setup = Join-Path $outRoot 'setup.exe'
    $hasSetup = Test-Path -LiteralPath $setup
} else {
    $packageDir  = $outRoot
    $packageName = $outRoot
    $settingsFile = Join-Path $packageDir 'appsettings.Local.json'
    $baseFile     = Join-Path $packageDir 'appsettings.json'
    $leaked = @(Get-ChildItem -LiteralPath $packageDir -File -Filter 'appsettings.Deploy.json*')
    $hasSetup = $true
}

$failures = New-Object System.Collections.Generic.List[string]
function Check([bool]$ok, [string]$okText, [string]$failText) {
    if ($ok) { Write-Host "  [OK]   $okText" } else { Write-Host "  [FAIL] $failText" -ForegroundColor Red; $failures.Add($failText) }
}

Check $hasSetup 'setup.exe present' 'setup.exe is missing from the output.'
Check (-not $leaked) 'appsettings.Deploy.json itself is not in the package' 'appsettings.Deploy.json was copied into the package under its own name.'
Check (Test-Path -LiteralPath $settingsFile) 'package contains the clinic settings file' "package has no $(Split-Path -Leaf $settingsFile)"

$server = $null
if (Test-Path -LiteralPath $settingsFile) {
    $packaged = Read-PantheonDb $settingsFile
    Check ($null -ne $packaged) 'packaged settings define ConnectionStrings:PantheonDB' 'packaged settings have no ConnectionStrings:PantheonDB'
    if ($packaged) {
        $server = $packaged.Server
        Check (-not $packaged.ConnectionString.Contains('placeholder.invalid')) 'no placeholder values' 'packaged settings still contain placeholder values'
        Check $packaged.HasPassword 'a password is set (value not shown)' 'packaged connection string has no password'
        if (Test-Path -LiteralPath $localFile) {
            $dev = Read-PantheonDb $localFile
            if ($dev) { Check ($packaged.Server -ne $dev.Server) 'server differs from your dev server' "package points at the dev server ($($packaged.Server))" }
        }
    }
}

if (Test-Path -LiteralPath $baseFile) {
    $base = Get-Content -LiteralPath $baseFile -Raw
    Check (-not [regex]::IsMatch($base, 'Password=[^;"]+')) 'packaged appsettings.json holds no password' 'packaged appsettings.json contains a password value'
}

Write-Host ''
if ($failures.Count -gt 0) {
    throw "Package check FAILED ($($failures.Count) problem(s)). Do NOT install this package."
}

Write-Host 'Package check passed.' -ForegroundColor Green
Write-Host "  Package:  $packageName"
Write-Host "  Server:   $server"
Write-Host "  Commit:   $commit"
if ($Mode -eq 'ClickOnce') { Write-Host "  Install from: $(Join-Path $outRoot 'setup.exe')" }
if ($isTest) { Write-Host '  (test publish: revision in the profile was not changed)' }
