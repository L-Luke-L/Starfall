param([string]$UnityManaged,[switch]$CompileAll,[string]$Unity='D:\Unity\6000.3.24f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$guidanceRoot=Split-Path -Parent $PSScriptRoot
$guidanceData=if($UnityManaged){Split-Path -Parent (Split-Path -Parent $UnityManaged)}else{Join-Path (Split-Path -Parent $Unity) 'Data'}
if(-not $UnityManaged){$UnityManaged=Join-Path $guidanceData 'Managed/UnityEngine'}
$guidanceTests=Join-Path $guidanceRoot 'Builds/GuidanceRuleTests'
New-Item -ItemType Directory -Path $guidanceTests -Force | Out-Null
if(-not (Test-Path -LiteralPath (Join-Path $UnityManaged 'UnityEngine.CoreModule.dll'))){throw 'Supply -UnityManaged pointing at an installed Unity Managed/UnityEngine directory.'}
$guidanceSources=@('Assets/Scripts/Gameplay/CampaignData.cs','Assets/Scripts/Gameplay/SubsurfaceDefinition.cs','Assets/Scripts/Combat/BasinProgress.cs','Assets/Scripts/Gameplay/FrontierCatalog.cs','Assets/Scripts/Gameplay/LostRealmCatalog.cs','Assets/Scripts/Gameplay/CampaignGuidance.cs','Assets/Scripts/Verification/GuidanceRuleChecks.cs','Tools/GuidanceTestMain.cs')
$guidanceDotnet=Join-Path $guidanceData 'NetCoreRuntime/dotnet.exe'
$guidanceCompiler=Join-Path $guidanceData 'DotNetSdkRoslyn/csc.dll'
$guidanceMono=Join-Path $guidanceData 'MonoBleedingEdge/bin/mono.exe'
foreach($guidanceTool in @($guidanceDotnet,$guidanceCompiler,$guidanceMono)){if(-not(Test-Path -LiteralPath $guidanceTool)){throw ('Missing Unity tool: '+$guidanceTool+'. Supply -Unity or -UnityManaged from a complete Unity Editor installation.')}}
$guidanceExe=Join-Path $guidanceTests 'GuidanceTests.exe'
$guidanceArgs=@('/nologo','/target:exe','/main:GuidanceTestMain','/nostdlib+',('/out:'+$guidanceExe))
$guidanceArgs+=Get-ChildItem -LiteralPath (Join-Path $guidanceData 'UnityReferenceAssemblies/unity-4.8-api') -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
$guidanceArgs+=Get-ChildItem -LiteralPath (Join-Path $guidanceData 'UnityReferenceAssemblies/unity-4.8-api/Facades') -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
if($CompileAll){$guidanceArgs+=Get-ChildItem -LiteralPath $UnityManaged -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName };$guidanceArgs+=Get-ChildItem -LiteralPath (Join-Path $guidanceRoot 'Assets/Scripts') -Recurse -Filter '*.cs' | ForEach-Object FullName;$guidanceArgs+=Join-Path $guidanceRoot 'Tools/GuidanceTestMain.cs'}else{$guidanceArgs+='/reference:'+(Join-Path $UnityManaged 'UnityEngine.CoreModule.dll');$guidanceArgs+=$guidanceSources | ForEach-Object { Join-Path $guidanceRoot $_ }}
$guidanceResponse=Join-Path $guidanceTests 'compile.rsp'
$guidanceArgs | ForEach-Object { '"'+$_+'"' } | Set-Content -LiteralPath $guidanceResponse
& $guidanceDotnet $guidanceCompiler ('@'+$guidanceResponse)
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$guidancePreviousMonoPath=$env:MONO_PATH
try{$env:MONO_PATH=$UnityManaged;& $guidanceMono $guidanceExe;$guidanceExitCode=$LASTEXITCODE}
finally{$env:MONO_PATH=$guidancePreviousMonoPath}
exit $guidanceExitCode
