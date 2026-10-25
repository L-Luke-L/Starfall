param([string[]]$Suites=@('guidanceQA','campaignQA','lostRealmQA','frontierQA'),[string]$Label='guidance-final')
$ErrorActionPreference='Stop'
$guidanceRoot=Split-Path -Parent $PSScriptRoot
$guidanceWorkspace=Join-Path $guidanceRoot 'Builds/GuidanceWorkspace-20261025'
$guidanceExe=Join-Path $guidanceWorkspace 'Builds/Windows/Starfall.exe'
$guidanceReports=@{guidanceQA='Guidance/guidance-report.txt';campaignQA='Campaign/campaign-report.txt';lostRealmQA='LostRealms/lost-realms-report.txt';frontierQA='Frontier/frontier-report.txt'}
$guidanceOut=Join-Path $guidanceRoot 'QA/Reports/GuidanceDelivery'
New-Item -ItemType Directory -Path $guidanceOut -Force | Out-Null
$guidanceResults=@();$guidanceFailures=0
foreach($guidanceSuite in $Suites){
 if(-not $guidanceReports.ContainsKey($guidanceSuite)){throw ('Unknown suite: '+$guidanceSuite)}
 $guidanceLog=Join-Path $guidanceRoot ('QA/Logs/'+$Label+'-'+$guidanceSuite+'.log')
 $guidanceProcess=Start-Process -FilePath $guidanceExe -ArgumentList @(('-'+$guidanceSuite),'-guidanceSave','-force-d3d11','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',('"'+$guidanceLog+'"')) -WindowStyle Hidden -PassThru -Wait
 $guidanceReport=Join-Path $guidanceWorkspace ('QA/Reports/'+$guidanceReports[$guidanceSuite])
 $guidanceLines=if(Test-Path -LiteralPath $guidanceReport){@(Get-Content -LiteralPath $guidanceReport)}else{@()}
 $guidancePassed=@($guidanceLines | Where-Object { $_ -match '^PASS ' }).Count
 $guidanceFailed=@($guidanceLines | Where-Object { $_ -match '^FAIL ' })
 $guidanceLogText=Get-Content -Raw -LiteralPath $guidanceLog
 $guidanceMarker=[regex]::IsMatch($guidanceLogText,'(?m)^(GUIDANCE|CAMPAIGN|LOST_REALM|FRONTIER)_QA_PASS\s*$')
 $guidanceResult=[pscustomobject]@{Suite=$guidanceSuite;ExitCode=$guidanceProcess.ExitCode;Passed=$guidancePassed;Failed=$guidanceFailed.Count;SuccessMarker=$guidanceMarker;Failures=$guidanceFailed;Report=$guidanceReport;Log=$guidanceLog}
 $guidanceResults+=$guidanceResult
 Write-Output ($guidanceSuite+': '+$guidancePassed+' PASS / '+$guidanceFailed.Count+' FAIL; exit '+$guidanceProcess.ExitCode+'; success marker '+$guidanceMarker)
 $guidanceFailed | Write-Output
 if($guidanceProcess.ExitCode -ne 0 -or $guidanceFailed.Count -gt 0 -or -not $guidanceMarker){$guidanceFailures++}
 $guidanceResults | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $guidanceOut ($Label+'-results.json'))
}
exit $(if($guidanceFailures -eq 0){0}else{2})
