[CmdletBinding()]
param(
 [string]$Unity='D:\Unity\6000.3.24f1\Editor\Unity.exe',
 [switch]$Build,
 [switch]$RunQA,
 [switch]$LifecycleOnly
)
$ErrorActionPreference='Stop'
$showcaseRoot=Split-Path -Parent $PSScriptRoot
$showcaseData=Join-Path (Split-Path -Parent $Unity) 'Data'
$showcaseOutput=Join-Path $showcaseRoot 'Builds/ShowcaseChecks'
$showcaseLogs=Join-Path $showcaseRoot 'QA/Logs'
$showcaseManaged=Join-Path $showcaseData 'Managed/UnityEngine'
$showcaseCompiler=Join-Path $showcaseData 'DotNetSdkRoslyn/csc.dll'
$showcaseDotnet=Join-Path $showcaseData 'NetCoreRuntime/dotnet.exe'
$showcaseMono=Join-Path $showcaseData 'MonoBleedingEdge/bin/mono.exe'
foreach($showcaseRequired in @($Unity,$showcaseCompiler,$showcaseDotnet,$showcaseMono)){
 if(-not (Test-Path -LiteralPath $showcaseRequired)){throw ('Missing Unity tool: '+$showcaseRequired)}
}
New-Item -ItemType Directory -Path $showcaseOutput,$showcaseLogs -Force | Out-Null
$showcaseExe=Join-Path $showcaseOutput 'ShowcaseChecks.exe'
$showcaseArguments=@('/nologo','/target:exe','/main:ShowcaseTestMain','/langversion:latest','/nostdlib+',('/out:'+$showcaseExe))
$showcaseArguments+=Get-ChildItem -LiteralPath (Join-Path $showcaseData 'UnityReferenceAssemblies/unity-4.8-api') -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
$showcaseArguments+=Get-ChildItem -LiteralPath (Join-Path $showcaseData 'UnityReferenceAssemblies/unity-4.8-api/Facades') -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
$showcaseArguments+=Get-ChildItem -LiteralPath $showcaseManaged -Filter '*.dll' | ForEach-Object { '/reference:'+ $_.FullName }
$showcaseArguments+=Get-ChildItem -LiteralPath (Join-Path $showcaseRoot 'Assets/Scripts') -Recurse -Filter '*.cs' | ForEach-Object FullName
$showcaseArguments+=Join-Path $showcaseRoot 'Tools/ShowcaseTestMain.cs'
$showcaseResponse=Join-Path $showcaseOutput 'compile.rsp'
$showcaseArguments | ForEach-Object { '"'+$_+'"' } | Set-Content -LiteralPath $showcaseResponse
& $showcaseDotnet $showcaseCompiler ('@'+$showcaseResponse)
if($LASTEXITCODE -ne 0){throw 'Runtime compilation failed.'}
$showcasePreviousMonoPath=$env:MONO_PATH
try{$env:MONO_PATH=$showcaseManaged;& $showcaseMono $showcaseExe;if($LASTEXITCODE -ne 0){throw 'Managed showcase checks failed.'}}
finally{$env:MONO_PATH=$showcasePreviousMonoPath}
if($Build){
 # Some desktop task hosts omit this standard Windows variable. UPM requires it
 # even for projects containing only built-in modules. This affects this process only.
 if(-not $env:ALLUSERSPROFILE){$env:ALLUSERSPROFILE=[Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData)}
 $showcaseBuildLog=Join-Path $showcaseLogs 'showcase-build.log'
 $showcaseBuildArgs=@('-batchmode','-nographics','-quit','-projectPath',('"'+$showcaseRoot+'"'),'-buildTarget','Win64','-executeMethod','StarfallBuilder.Build','-logFile',('"'+$showcaseBuildLog+'"'))
 $showcaseProcess=Start-Process -FilePath $Unity -ArgumentList $showcaseBuildArgs -WindowStyle Hidden -PassThru
 if(-not $showcaseProcess.WaitForExit(1200000)){Stop-Process -Id $showcaseProcess.Id;throw ('Unity build exceeded 20 minutes; see '+$showcaseBuildLog)}
 if($showcaseProcess.ExitCode -ne 0){throw ('Unity build failed; see '+$showcaseBuildLog)}
}
if($RunQA){
 $showcasePlayer=Join-Path $showcaseRoot 'Builds/Windows/Starfall.exe'
 if(-not (Test-Path -LiteralPath $showcasePlayer)){throw 'Run with -Build first, or build the current source with Unity.'}
 $showcaseAssembly=Get-Item -LiteralPath (Join-Path $showcaseRoot 'Builds/Windows/Starfall_Data/Managed/Assembly-CSharp.dll')
 $showcaseNewestSource=Get-ChildItem -LiteralPath (Join-Path $showcaseRoot 'Assets/Scripts') -Recurse -Filter '*.cs' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
 if($showcaseAssembly.LastWriteTimeUtc -lt $showcaseNewestSource.LastWriteTimeUtc){throw 'The Windows player predates runtime source changes. Run again with -Build -RunQA to verify the current implementation.'}
 $showcasePlayerLog=Join-Path $showcaseLogs $(if($LifecycleOnly){'showcase-lifecycle-player.log'}else{'showcase-player.log'})
 $showcasePlayerArgs=@('-showcaseQA','-force-d3d11','-force-d3d11-bitblt-model','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+$showcasePlayerLog+'"'))
 if($LifecycleOnly){$showcasePlayerArgs+='-showcaseLifecycleOnly'}
 # OnGUI frame capture needs a visible swap chain.
 $showcaseProcess=Start-Process -FilePath $showcasePlayer -ArgumentList $showcasePlayerArgs -WindowStyle Normal -PassThru
 if(-not $showcaseProcess.WaitForExit(780000)){Stop-Process -Id $showcaseProcess.Id;throw ('Showcase verification exceeded 13 minutes; see '+$showcasePlayerLog)}
 if($showcaseProcess.ExitCode -ne 0){throw ('Showcase runtime verification failed; see '+$showcasePlayerLog)}
 if(-not (Select-String -LiteralPath $showcasePlayerLog -Pattern '^SHOWCASE_QA_PASS' -Quiet)){throw ('Player exited without a successful showcase report; see '+$showcasePlayerLog)}
}
Write-Output 'SHOWCASE_CHECKS_PASS'
