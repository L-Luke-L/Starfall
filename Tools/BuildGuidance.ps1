param([string]$Label='guidance',[string]$Unity='E:/Unity/Hub/Editor/2022.3.35f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$guidanceRoot=Split-Path -Parent $PSScriptRoot
$guidanceWorkspace=Join-Path $guidanceRoot 'Builds/GuidanceWorkspace-20261025'
$guidanceLogRoot=Join-Path $guidanceRoot 'QA/Logs'
New-Item -ItemType Directory -Path $guidanceWorkspace,$guidanceLogRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $guidanceWorkspace 'QA') -Force | Out-Null
foreach($guidanceFolder in @('Assets','Packages','ProjectSettings')){
 & robocopy (Join-Path $guidanceRoot $guidanceFolder) (Join-Path $guidanceWorkspace $guidanceFolder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw ('Copy failed: '+$guidanceFolder)}
}
if(-not (Test-Path -LiteralPath (Join-Path $guidanceWorkspace 'Library'))){
 $guidanceLibrary=Join-Path $guidanceRoot 'Builds/BuildWorkspace-20261025/Library'
 if(Test-Path -LiteralPath $guidanceLibrary){& robocopy $guidanceLibrary (Join-Path $guidanceWorkspace 'Library') /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null;if($LASTEXITCODE -ge 8){throw 'Library seed copy failed'}}
}
Set-Content -LiteralPath (Join-Path $guidanceWorkspace 'ProjectSettings/ProjectVersion.txt') -Value "m_EditorVersion: 2022.3.35f1`nm_EditorVersionWithRevision: 2022.3.35f1 (011206c7a712)"
$guidanceLog=Join-Path $guidanceLogRoot ($Label+'-build.log')
$guidanceArguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$guidanceWorkspace+'"'),'-buildTarget','Win64','-executeMethod','StarfallBuilder.Build','-logFile',('"'+$guidanceLog+'"'))
$guidanceProcess=Start-Process -FilePath $Unity -ArgumentList $guidanceArguments -WindowStyle Hidden -PassThru -Wait
Write-Output ('UNITY_EXIT='+$guidanceProcess.ExitCode)
Get-Content -LiteralPath $guidanceLog -Tail 12
exit $guidanceProcess.ExitCode
