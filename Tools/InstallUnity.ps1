[CmdletBinding()]
param(
    [string]$InstallRoot = 'D:\Unity\6000.3.24f1',
    [string]$DownloadRoot = 'D:\Unity\Downloads',
    [switch]$DownloadOnly
)

# Release and installer arguments are documented by Unity:
# https://unity.com/releases/editor/whats-new/6000.3.24f1
# https://docs.unity3d.com/6000.0/Documentation/Manual/InstallingUnity.html
$ErrorActionPreference = 'Stop'
$version = '6000.3.24f1'
$revision = '4e7b9b5b6244'
$downloadUrl = "https://download.unity3d.com/download_unity/$revision/Windows64EditorInstaller/UnitySetup64-$version.exe"
$expectedBytes = 4127507400L
$expectedMd5 = '10D8EBFF331420FC53E7D6D59D0555F2'
$installer = Join-Path $DownloadRoot "UnitySetup64-$version.exe"
$editor = Join-Path $InstallRoot 'Editor\Unity.exe'

$projectVersionPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'ProjectSettings\ProjectVersion.txt'
if ((Get-Content -LiteralPath $projectVersionPath -Raw) -notmatch "m_EditorVersion: $([regex]::Escape($version))\s") {
    throw 'The project Unity version changed. Update this installer explicitly before continuing.'
}

New-Item -ItemType Directory -Path $InstallRoot, $DownloadRoot -Force | Out-Null

for ($attempt = 1; $attempt -le 8; $attempt++) {
    if ((Test-Path -LiteralPath $installer) -and (Get-Item -LiteralPath $installer).Length -eq $expectedBytes) {
        break
    }
    Write-Host "Downloading Unity $version from its official release to $installer (attempt $attempt/8)"
    & curl.exe --location --fail --retry 5 --retry-delay 3 --connect-timeout 30 --continue-at - --output $installer $downloadUrl
    if ($LASTEXITCODE -ne 0 -and $attempt -eq 8) {
        throw "Unity download failed (curl exit code $LASTEXITCODE). Run this script again to resume."
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'The connection was interrupted. Resuming from the bytes already downloaded.'
        Start-Sleep -Seconds 3
    }
}

if ((Get-Item -LiteralPath $installer).Length -ne $expectedBytes) {
    throw 'The downloaded installer size does not match the official release.'
}
if ((Get-FileHash -LiteralPath $installer -Algorithm MD5).Hash -ne $expectedMd5) {
    throw 'The downloaded installer does not match the MD5 published in the official server ETag.'
}
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Unity Technologies') {
    throw "Unity installer signature validation failed: $($signature.Status)."
}
Get-FileHash -LiteralPath $installer -Algorithm SHA256 |
    Format-List | Out-File -LiteralPath (Join-Path $DownloadRoot "UnitySetup64-$version.sha256.txt") -Encoding utf8
Write-Host "Verified official installer signed by $($signature.SignerCertificate.Subject)."

if ($DownloadOnly) {
    return
}
if (!(Test-Path -LiteralPath $editor)) {
    Write-Host "Installing Unity into $InstallRoot"
    # Unity documents /S for silent installation and requires /D to be the final argument.
    $installation = Start-Process -FilePath $installer -ArgumentList @('/S', "/D=$InstallRoot") -WindowStyle Hidden -Wait -PassThru
    if ($installation.ExitCode -ne 0) {
        throw "Unity installation returned exit code $($installation.ExitCode)."
    }
}
if (!(Test-Path -LiteralPath $editor)) {
    throw "Unity Editor was not found at $editor after installation."
}
$installedSignature = Get-AuthenticodeSignature -LiteralPath $editor
if ($installedSignature.Status -ne 'Valid' -or $installedSignature.SignerCertificate.Subject -notmatch 'Unity Technologies') {
    throw "Installed Unity Editor signature validation failed: $($installedSignature.Status)."
}
Write-Host "Unity $version installed at $editor"
Write-Host 'The editor may require a Unity account and a valid license on first launch. This script does not sign in or activate a license.'
