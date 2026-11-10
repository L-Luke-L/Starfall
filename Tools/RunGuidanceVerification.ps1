param([string[]]$Suites=@('guidanceQA','campaignQA','lostRealmQA','frontierQA'),[string]$Label='guidance-final')
$ErrorActionPreference='Stop'
# Keep the original invocation while using the current player, isolated QA saves,
# bounded process waits and fresh-report validation shared by the audit runner.
& (Join-Path $PSScriptRoot 'TestProjectAudit.ps1') -Suites $Suites -Label $Label
exit $LASTEXITCODE
