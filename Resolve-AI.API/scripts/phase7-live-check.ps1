# Phase 7 live check (read-only).
#
# Calls the running API and checks the steps that can be verified on the real
# Microsoft 365 tenant. Nothing is changed: every workflow call uses
# isApproved = false, so no license is assigned and no email or mailbox
# change is made. Assignment (WBS 7.08, 7.09) is NOT covered here.
#
# Usage (API must be running, started from the project folder):
#   powershell -ExecutionPolicy Bypass -File .\scripts\phase7-live-check.ps1
#   powershell -ExecutionPolicy Bypass -File .\scripts\phase7-live-check.ps1 -BaseUrl http://localhost:5085

param(
    [string]$BaseUrl = "http://localhost:5085",
    [string]$KnownUser = "HassanKhan@SystemCastle1.onmicrosoft.com"
)

$ErrorActionPreference = "Stop"
$results = New-Object System.Collections.Generic.List[object]

function Invoke-Api {
    param([string]$Method, [string]$Path, [object]$Body)

    $uri = "$BaseUrl$Path"
    try {
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Method Get -Uri $uri
        }
        else {
            $json = $Body | ConvertTo-Json -Depth 5
            $response = Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $json
        }
        return [pscustomobject]@{ Ok = $true; Data = $response; Error = "" }
    }
    catch {
        $failed = [pscustomobject]@{
            status = "CALL FAILED"
            isAvailable = $null
            quotaEvaluation = $null
            alert = $null
            alertNotification = $null
            mailboxAction = $null
            message = $_.Exception.Message
        }
        return [pscustomobject]@{ Ok = $false; Data = $failed; Error = $_.Exception.Message }
    }
}

function New-Process {
    param([string]$RequestId, [string]$User, [int]$UseCase)

    return @{
        requestId      = $RequestId
        userIdentifier = $User
        useCase        = $UseCase
        isApproved     = $false
    }
}

function Add-Check {
    param(
        [string]$Number,
        [string]$Name,
        [bool]$Passed,
        [string]$PassText,
        [string]$FailText
    )

    $results.Add([pscustomobject]@{
        Number = $Number
        Name   = $Name
        Result = if ($Passed) { "PASS" } else { "FAIL" }
        Detail = if ($Passed) { $PassText } else { $FailText }
    })
}

# Stop early with a clear message when the API is not running.
$probe = Invoke-Api -Method GET -Path "/api/phase7-test/microsoft365"
if (-not $probe.Ok) {
    Write-Host "The API is not running at $BaseUrl."
    Write-Host "Start it first from the project folder: dotnet run --urls $BaseUrl"
    Write-Host "Error: $($probe.Error)"
    exit 2
}

# 1. Microsoft 365 connection (WBS 7.04)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-704" $KnownUser 7)
$ok = -not ("$($r.Data.status)" -match "^M365_UNAVAILABLE|^CALL FAILED")
Add-Check "1" "Microsoft 365 connection" $ok `
    "Connected to Microsoft 365." `
    "Could not connect to Microsoft 365. Status: $($r.Data.status). $($r.Data.message)"

# 2-4. Request validation (WBS 7.03)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body @{ requestId = ""; userIdentifier = $KnownUser; useCase = 7 }
$ok = $r.Data.status -eq "VALIDATION_FAILED"
Add-Check "2" "Empty Request ID is rejected" $ok `
    "Request was rejected, as expected." `
    "Request was accepted (status: $($r.Data.status)). This is wrong."

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body @{ requestId = "LIVE-703b"; userIdentifier = ""; useCase = 7 }
$ok = $r.Data.status -eq "VALIDATION_FAILED"
Add-Check "3" "Empty user name is rejected" $ok `
    "Request was rejected, as expected." `
    "Request was accepted (status: $($r.Data.status)). This is wrong."

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-703c" $KnownUser 5)
$ok = $r.Data.status -eq "VALIDATION_FAILED"
Add-Check "4" "Wrong use case (5) is rejected" $ok `
    "Request was rejected, as expected." `
    "Request was accepted (status: $($r.Data.status)). This is wrong."

# 5-6. User lookup (WBS 7.05)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-705a" "nobody.live.check@SystemCastle1.onmicrosoft.com" 7)
$ok = $r.Data.status -eq "USER_NOT_FOUND"
Add-Check "5" "User who is not in the tenant" $ok `
    "System reported that the user was not found." `
    "System said: $($r.Data.status). Expected USER_NOT_FOUND."

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-705b" $KnownUser 7)
$ok = -not ("$($r.Data.status)" -match "^USER_NOT_FOUND|^USER_DISABLED|^CALL FAILED")
Add-Check "6" "HassanKhan exists and is enabled" $ok `
    "User found, account enabled." `
    "User not found or disabled. Status: $($r.Data.status)."

# 7. License status (WBS 7.06 and 7.07)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-707" $KnownUser 7)
$ok = $r.Data.status -eq "NO_ACTION_REQUIRED"
Add-Check "7" "HassanKhan license" $ok `
    "Business Premium and Teams already active. No new license assigned." `
    "Expected the license to be already active (NO_ACTION_REQUIRED), got: $($r.Data.status)."

# 8-9. Mailbox usage and capacity (WBS 7.11 and 7.12)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-711" $KnownUser 8)
$ok = -not ("$($r.Data.status)" -match "^MAILBOX_DATA_NOT_FOUND|^CALL FAILED")
Add-Check "8" "HassanKhan mailbox data" $ok `
    "Mailbox usage data found." `
    "No mailbox usage data. Status: $($r.Data.status)."

$percent = $r.Data.quotaEvaluation.usagePercent
$state = $r.Data.quotaEvaluation.capacityState
$ok = $r.Data.status -eq "CAPACITY_ANALYSED"
Add-Check "9" "HassanKhan mailbox capacity" $ok `
    "Mailbox is $percent% full (state $state)." `
    "Capacity could not be analysed. Status: $($r.Data.status)."

# 10. Alert decision (WBS 7.13)
$decision = $r.Data.quotaEvaluation.decision
$ok = $decision -eq "NO_ALERT"
Add-Check "10" "Alert decision for a normal mailbox" $ok `
    "Mailbox is normal, so no alert is needed." `
    "Expected NO_ALERT, got: $decision."

# 11-13. Nothing created or sent for a normal mailbox (WBS 7.13a, 7.13b, 7.14)
Add-Check "11" "No alert record for a normal mailbox" ($r.Ok -and $null -eq $r.Data.alert) `
    "No alert record was created." `
    "An alert record was created for a normal mailbox. This is wrong."

Add-Check "12" "No notification for a normal mailbox" ($r.Ok -and $null -eq $r.Data.alertNotification) `
    "No notification was sent." `
    "A notification was sent for a normal mailbox. This is wrong."

Add-Check "13" "No mailbox action for a normal mailbox" ($r.Ok -and $null -eq $r.Data.mailboxAction) `
    "No mailbox action ran." `
    "A mailbox action ran for a normal mailbox. This is wrong."

# 14. Exchange connection (WBS 7.10)
$r = Invoke-Api -Method GET -Path "/api/phase7-test/exchange"
$ok = $r.Data.isAvailable -eq $true
Add-Check "14" "Exchange connection" $ok `
    "Exchange connection is working." `
    "Could not connect to Exchange. $($r.Data.message)"

# Report
$passed = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$total = $results.Count

Write-Host ""
Write-Host "Phase 7 live check (real Microsoft 365 tenant, read-only)"
Write-Host "No license was assigned and no email or mailbox change was made."
Write-Host ""
foreach ($item in $results) {
    Write-Host ("{0,3}  {1,-40} {2,-5} {3}" -f $item.Number, $item.Name, $item.Result, $item.Detail)
}
Write-Host ""
Write-Host "Result: $passed of $total checks passed."
Write-Host "Not checked here: assignment and verification (WBS 7.08, 7.09, paid seat), and the alert and action steps for a WARNING or CRITICAL mailbox (WBS 7.13a, 7.13b, 7.14, 7.15), which are checked by the simulation in tools/Phase7Simulation."

if ($passed -ne $total) { exit 1 }
