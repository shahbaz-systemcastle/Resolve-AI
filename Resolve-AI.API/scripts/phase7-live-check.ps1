# Phase 7 live check (read-only).
#
# Calls the running API and checks the steps that can be verified on the real
# tenant without changing anything. Every workflow call uses isApproved = false,
# so no license is assigned. Assignment (7.08, 7.09) is NOT covered here.
#
# Usage (API must be running):
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
        return [pscustomobject]@{ Ok = $true; Code = 200; Data = $response; Error = "" }
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        # Keep the failure visible: the status says CALL FAILED, so no check can pass by accident.
        $failed = [pscustomobject]@{
            status = "CALL FAILED: $($_.Exception.Message)"
            isAvailable = $null
            quotaEvaluation = $null
            alert = $null
            alertNotification = $null
            mailboxAction = $null
        }
        return [pscustomobject]@{ Ok = $false; Code = $code; Data = $failed; Error = $_.Exception.Message }
    }
}

# Stop early with a clear message when the API is not running.
$probe = Invoke-Api -Method GET -Path "/api/phase7-test/microsoft365"
if (-not $probe.Ok) {
    Write-Host "The API is not reachable at $BaseUrl. Start the API first (dotnet run --urls $BaseUrl) and run this again."
    Write-Host "Error: $($probe.Error)"
    exit 2
}

function Add-Check {
    param([string]$Step, [string]$Name, [string]$Expected, [string]$Actual, [bool]$Pass)

    $results.Add([pscustomobject]@{
        Step     = $Step
        Check    = $Name
        Expected = $Expected
        Actual   = $Actual
        Result   = if ($Pass) { "PASS" } else { "FAIL" }
    })
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

# 7.04 - Microsoft 365 availability (real Graph call through the workflow)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-704" $KnownUser 7)
Add-Check "7.04" "Microsoft 365 reachable" "not M365_UNAVAILABLE" `
    ($r.Data.status) (-not ("$($r.Data.status)" -match "^M365_UNAVAILABLE|^CALL FAILED"))

# 7.03 - Request validation
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body @{ requestId = ""; userIdentifier = $KnownUser; useCase = 7 }
Add-Check "7.03" "Empty RequestId rejected" "VALIDATION_FAILED" "$($r.Data.status)" ($r.Data.status -eq "VALIDATION_FAILED")

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body @{ requestId = "LIVE-703b"; userIdentifier = ""; useCase = 7 }
Add-Check "7.03" "Empty UserIdentifier rejected" "VALIDATION_FAILED" "$($r.Data.status)" ($r.Data.status -eq "VALIDATION_FAILED")

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-703c" $KnownUser 5)
Add-Check "7.03" "Unsupported UseCase rejected" "VALIDATION_FAILED" "$($r.Data.status)" ($r.Data.status -eq "VALIDATION_FAILED")

# 7.05 - User lookup
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-705a" "nobody.live.check@SystemCastle1.onmicrosoft.com" 7)
Add-Check "7.05" "Unknown user gives USER_NOT_FOUND" "USER_NOT_FOUND" "$($r.Data.status)" ($r.Data.status -eq "USER_NOT_FOUND")

$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-705b" $KnownUser 7)
Add-Check "7.05" "Known user found and enabled" "not USER_NOT_FOUND / USER_DISABLED" "$($r.Data.status)" `
    (-not ("$($r.Data.status)" -match "^USER_NOT_FOUND|^USER_DISABLED|^CALL FAILED"))

# 7.06 - 7.07 - License status and validation (UC-07, not approved, so nothing is assigned)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-707" $KnownUser 7)
Add-Check "7.06-7.07" "Known user already licensed (no assignment needed)" "NO_ACTION_REQUIRED" "$($r.Data.status)" ($r.Data.status -eq "NO_ACTION_REQUIRED")

# 7.11 - 7.12 - Mailbox usage and capacity analysis (UC-08)
$r = Invoke-Api -Method POST -Path "/api/phase7-test/process" -Body (New-Process "LIVE-711" $KnownUser 8)
Add-Check "7.11" "Mailbox usage found" "not MAILBOX_DATA_NOT_FOUND" "$($r.Data.status)" (-not ("$($r.Data.status)" -match "^MAILBOX_DATA_NOT_FOUND|^CALL FAILED"))
Add-Check "7.12" "Capacity analysed" "CAPACITY_ANALYSED" "$($r.Data.status)" ($r.Data.status -eq "CAPACITY_ANALYSED")

# 7.13 - Capacity validation (quota decision for a NORMAL mailbox)
$decision = $r.Data.quotaEvaluation.decision
Add-Check "7.13" "NORMAL mailbox gives NO_ALERT" "NO_ALERT" "$decision" ($decision -eq "NO_ALERT")

# 7.13a / 7.13b / 7.14 - nothing should be created for a NORMAL mailbox
Add-Check "7.13a" "No alert created for NORMAL mailbox" "null" "$($r.Data.alert)" ($r.Ok -and $null -eq $r.Data.alert)
Add-Check "7.13b" "No notification for NORMAL mailbox" "null" "$($r.Data.alertNotification)" ($r.Ok -and $null -eq $r.Data.alertNotification)
Add-Check "7.14" "No mailbox action for NORMAL mailbox" "null" "$($r.Data.mailboxAction)" ($r.Ok -and $null -eq $r.Data.mailboxAction)

# 7.10 - Exchange check (separate test endpoint)
$r = Invoke-Api -Method GET -Path "/api/phase7-test/exchange"
Add-Check "7.10" "Exchange connection verified" "isAvailable = true" "$($r.Data.isAvailable)" ($r.Data.isAvailable -eq $true)

# Summary
Write-Host ""
$results | Format-Table -AutoSize Step, Check, Expected, Actual, Result

$passed = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$total = $results.Count
Write-Host "Result: $passed of $total checks passed."
Write-Host "Not covered here: 7.08 assignment and 7.09 verification (paid seat), and 7.13a/7.13b/7.14/7.15 alert and action paths (need simulation)."

if ($passed -ne $total) { exit 1 }
