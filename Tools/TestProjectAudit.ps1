[CmdletBinding()]
param(
 [string[]]$Suites=@('lostRealmQA','frontierQA','interfaceQA','guidanceQA','creatureQA','monsterVisibilityQA','hardQA','forgeQA'),
 [string]$Label='audit',
 [int]$TimeoutSeconds=420
)
$ErrorActionPreference='Stop'
$auditRoot=Split-Path -Parent $PSScriptRoot
$auditPlayer=Join-Path $auditRoot 'Builds/Windows/Starfall.exe'
if(-not(Test-Path -LiteralPath $auditPlayer)){throw 'Build the Windows player first.'}
if($Label -notmatch '^[a-zA-Z0-9-]+$'){throw 'Label must contain letters, numbers and hyphens only.'}
$auditOutput=Join-Path $auditRoot ('QA/Reports/ProjectAudit/'+$Label)
$auditLogs=Join-Path $auditRoot 'QA/Logs'
New-Item -ItemType Directory -Path $auditOutput,$auditLogs -Force | Out-Null
$auditSuiteMap=@{
 projectAuditQA=@('Reports/ProjectAudit/Targeted/audit-report.txt','PROJECT_AUDIT_PASS')
 showcaseQA=@('Reports/Showcase/showcase-report.txt','SHOWCASE_QA_PASS')
 lostRealmQA=@('Reports/LostRealms/lost-realms-report.txt','LOST_REALM_QA_PASS')
 frontierQA=@('Reports/Frontier/frontier-report.txt','FRONTIER_QA_PASS')
 interfaceQA=@('Reports/Interface/interface-report.txt','STARFALL_INTERFACE_QA_PASS')
 guidanceQA=@('Reports/Guidance/guidance-report.txt','GUIDANCE_QA_PASS')
 campaignQA=@('Reports/Campaign/campaign-report.txt','CAMPAIGN_QA_PASS')
 creatureQA=@('creatures-v10/creature-report.json','STARFALL_CREATURE_QA_PASS')
 monsterVisibilityQA=@('Reports/MonsterVisibility/gameplay-report.txt','STARFALL_MONSTER_VISIBILITY_QA_PASS')
 hardQA=@('combat-v14/hard-report.json','STARFALL_HARD_QA_PASS')
 forgeQA=@('forge-v8/forge-report.json','STARFALL_FORGE_QA_PASS')
 immersionQA=@('immersion-v15/immersion-report.json','STARFALL_IMMERSION_QA_PASS')
 battleQA=@('first-person-v9/battle-report.json','STARFALL_BATTLE_QA_PASS')
 dragonQA=@('dragon-v13/dragon-report.json','STARFALL_DRAGON_QA_PASS')
 encounterQA=@('encounter-v7/encounter-report.json','STARFALL_ENCOUNTER_QA_PASS')
 titanQA=@('titan-v11/titan-report.json','STARFALL_TITAN_QA_PASS')
 portalQA=@('wormhole-v12/wormhole-report.json','STARFALL_WORMHOLE_QA_PASS')
 portalReloadQA=@('wormhole-v12/reload-report.json','STARFALL_WORMHOLE_QA_PASS')
 undergroundQA=@('underground-v3/underground-report.json','STARFALL_UNDERGROUND_QA_PASS')
}
$auditResults=@();$auditFailureCount=0
$auditAssembly=Get-FileHash -LiteralPath (Join-Path $auditRoot 'Builds/Windows/Starfall_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256
$auditNewestSource=Get-ChildItem -LiteralPath (Join-Path $auditRoot 'Assets/Scripts') -Recurse -Filter '*.cs' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if((Get-Item -LiteralPath $auditAssembly.Path).LastWriteTimeUtc -lt $auditNewestSource.LastWriteTimeUtc){throw 'The Windows player predates runtime changes. Rebuild with Tools/TestShowcase.ps1 -Build first.'}
foreach($auditSuite in $Suites){
 if(-not $auditSuiteMap.ContainsKey($auditSuite)){throw ('Unknown suite: '+$auditSuite)}
 $auditLog=Join-Path $auditLogs ($Label+'-'+$auditSuite+'.log')
 $auditReport=Join-Path $auditRoot ('QA/'+$auditSuiteMap[$auditSuite][0])
 $auditStarted=[DateTime]::UtcNow
 $auditArguments=@(('-'+$auditSuite),'-guidanceSave','-force-d3d11','-force-d3d11-bitblt-model','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+$auditLog+'"'))
 $auditProcess=Start-Process -FilePath $auditPlayer -ArgumentList $auditArguments -WindowStyle Hidden -PassThru
 $auditSuiteTimeout=if($auditSuite -eq 'showcaseQA'){[Math]::Max(660,$TimeoutSeconds)}else{$TimeoutSeconds}
 $auditTimedOut=-not $auditProcess.WaitForExit($auditSuiteTimeout*1000)
 if($auditTimedOut){Stop-Process -Id $auditProcess.Id -ErrorAction SilentlyContinue;$auditProcess.WaitForExit()}
 $auditLogText=if(Test-Path -LiteralPath $auditLog){Get-Content -LiteralPath $auditLog -Raw}else{''}
 $auditFreshReport=(Test-Path -LiteralPath $auditReport) -and (Get-Item -LiteralPath $auditReport).LastWriteTimeUtc -ge $auditStarted.AddSeconds(-1)
 $auditPassed=0;$auditFailed=@()
 if($auditFreshReport){
  Copy-Item -LiteralPath $auditReport -Destination (Join-Path $auditOutput ($auditSuite+[IO.Path]::GetExtension($auditReport))) -Force
  if([IO.Path]::GetExtension($auditReport) -eq '.json'){
   $auditJson=Get-Content -LiteralPath $auditReport -Raw | ConvertFrom-Json
   foreach($auditProperty in $auditJson.PSObject.Properties){if($auditProperty.Name -eq 'passed'){continue};if($auditProperty.Value -is [bool]){if($auditProperty.Value){$auditPassed++}else{$auditFailed+=$auditProperty.Name}}}
  }else{$auditLines=Get-Content -LiteralPath $auditReport;$auditPassed=@($auditLines|Where-Object {$_ -match '^PASS '}).Count;$auditFailed=@($auditLines|Where-Object {$_ -match '^FAIL '})}
 }
 $auditMarker=[regex]::IsMatch($auditLogText,'(?m)^'+[regex]::Escape($auditSuiteMap[$auditSuite][1])+'\s*$')
 $auditExceptions=@([regex]::Matches($auditLogText,'(?m)^(?:NullReferenceException|MissingReferenceException|IndexOutOfRangeException|InvalidOperationException|ArgumentException|ArgumentOutOfRangeException|StackOverflowException):[^\r\n]*')|ForEach-Object Value|Select-Object -Unique)
 $auditResult=[pscustomobject]@{Suite=$auditSuite;ExitCode=$auditProcess.ExitCode;TimedOut=$auditTimedOut;FreshReport=$auditFreshReport;Passed=$auditPassed;Failed=$auditFailed.Count;Failures=$auditFailed;SuccessMarker=$auditMarker;Exceptions=$auditExceptions;AssemblySHA256=$auditAssembly.Hash;Log=$auditLog}
 $auditResults+=$auditResult
 if($auditTimedOut -or $auditProcess.ExitCode -ne 0 -or -not $auditFreshReport -or $auditFailed.Count -gt 0 -or -not $auditMarker -or $auditExceptions.Count -gt 0){$auditFailureCount++}
 $auditResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $auditOutput 'results.json')
 Write-Output ($auditSuite+': '+$auditPassed+' PASS / '+$auditFailed.Count+' FAIL; exit '+$auditProcess.ExitCode+'; fresh='+$auditFreshReport+'; marker='+$auditMarker+'; exceptions='+$auditExceptions.Count)
 $auditFailed | Write-Output
 $auditExceptions | Write-Output
}
exit $(if($auditFailureCount -eq 0){0}else{2})
