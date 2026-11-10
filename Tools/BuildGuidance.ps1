param([string]$Label='guidance',[string]$Unity='D:\Unity\6000.3.24f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
if($Label -notmatch '^[a-zA-Z0-9-]+$'){throw 'Label must contain letters, numbers and hyphens only.'}
# Compatibility entry point: use the same engine and output as the current game.
# The former copy builder forcibly downgraded ProjectVersion.txt to Unity 2022.
& (Join-Path $PSScriptRoot 'TestShowcase.ps1') -Unity $Unity -Build
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$guidanceRoot=Split-Path -Parent $PSScriptRoot
$guidanceSourceLog=Join-Path $guidanceRoot 'QA/Logs/showcase-build.log'
$guidanceLabelLog=Join-Path $guidanceRoot ('QA/Logs/'+$Label+'-build.log')
if($guidanceSourceLog -ne $guidanceLabelLog){Copy-Item -LiteralPath $guidanceSourceLog -Destination $guidanceLabelLog -Force}
Write-Output ('GUIDANCE_BUILD_PASS '+(Join-Path $guidanceRoot 'Builds/Windows/Starfall.exe'))
