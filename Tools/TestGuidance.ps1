param([string]$UnityManaged='E:/Unity/Hub/Editor/2022.3.35f1/Editor/Data/Managed/UnityEngine',[switch]$CompileAll)
$ErrorActionPreference='Stop'
$guidanceRoot=Split-Path -Parent $PSScriptRoot
$guidanceTests=Join-Path $guidanceRoot 'Builds/GuidanceRuleTests'
New-Item -ItemType Directory -Path $guidanceTests -Force | Out-Null
if(-not (Test-Path -LiteralPath (Join-Path $UnityManaged 'UnityEngine.CoreModule.dll'))){throw 'Supply -UnityManaged pointing at an installed Unity Managed/UnityEngine directory.'}
$guidanceSources=@('Assets/Scripts/Gameplay/CampaignData.cs','Assets/Scripts/Gameplay/SubsurfaceDefinition.cs','Assets/Scripts/Combat/BasinProgress.cs','Assets/Scripts/Gameplay/FrontierCatalog.cs','Assets/Scripts/Gameplay/LostRealmCatalog.cs','Assets/Scripts/Gameplay/CampaignGuidance.cs','Assets/Scripts/Verification/GuidanceRuleChecks.cs','Tools/GuidanceTestMain.cs')
$guidanceDotnet=Split-Path -Parent (Get-Command dotnet).Source
$guidanceSdk=(& dotnet --version).Trim()
$guidanceRef=Get-ChildItem -LiteralPath (Join-Path $guidanceDotnet 'packs/Microsoft.NETCore.App.Ref') -Directory | Where-Object Name -Like '9.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if(-not $guidanceRef){throw 'A .NET 9 reference pack is required.'}
$guidanceArgs=@('/nologo','/target:exe','/main:GuidanceTestMain','/nostdlib+',('/out:'+(Join-Path $guidanceTests 'GuidanceTests.dll')))
$guidanceArgs+=Get-ChildItem -LiteralPath (Join-Path $guidanceRef.FullName 'ref/net9.0') -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
if($CompileAll){$guidanceArgs+=Get-ChildItem -LiteralPath $UnityManaged -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName };$guidanceArgs+=Get-ChildItem -LiteralPath (Join-Path $guidanceRoot 'Assets/Scripts') -Recurse -Filter '*.cs' | ForEach-Object FullName;$guidanceArgs+=Join-Path $guidanceRoot 'Tools/GuidanceTestMain.cs'}else{$guidanceArgs+='/reference:'+(Join-Path $UnityManaged 'UnityEngine.CoreModule.dll');$guidanceArgs+=$guidanceSources | ForEach-Object { Join-Path $guidanceRoot $_ }}
$guidanceResponse=Join-Path $guidanceTests 'compile.rsp'
$guidanceArgs | ForEach-Object { '"'+$_+'"' } | Set-Content -LiteralPath $guidanceResponse
& dotnet (Join-Path $guidanceDotnet ('sdk/'+$guidanceSdk+'/Roslyn/bincore/csc.dll')) ('@'+$guidanceResponse)
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Copy-Item -LiteralPath (Join-Path $UnityManaged 'UnityEngine.CoreModule.dll') -Destination $guidanceTests -Force
Set-Content -LiteralPath (Join-Path $guidanceTests 'GuidanceTests.runtimeconfig.json') -Value '{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.0"}}}'
& dotnet (Join-Path $guidanceTests 'GuidanceTests.dll')
exit $LASTEXITCODE
