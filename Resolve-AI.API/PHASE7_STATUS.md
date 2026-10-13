# Phase 7 – License & Mailbox Management Module: Status and Handoff

Last updated: 2026-10-13 (7.3 to 7.10 fixes applied, status codes added, UC-07 assignment tested on a test user)
Branch: `shahbaz/phase7-license-mailbox`
Stack: ASP.NET Core, .NET 10, layered (API → Application → Infrastructure, shared Domain)

> Purpose of this file: a single place to resume work. It separates what was
> **verified in a session** from what is **only visible in the code** and has
> not been tested.

---

## 1. What this module does

| Enum value | Use case | Goal |
|---|---|---|
| 7 | `Microsoft365LicenseNotAssigned` (UC-07) | Find the user, check for Business Premium with active Teams, assign it if approved, then verify it |
| 8 | `MailboxCapacityThresholdAlert` (UC-08) | Check Exchange connection, read mailbox usage, analyse capacity |

All other use cases (1–6, 9–11) are rejected at validation as "not supported by Phase 7".

Microsoft 365 data comes from **Microsoft Graph (Entra ID)**, not on-prem Active Directory.
Mailbox and Exchange checks use Exchange Online PowerShell.

Assumed final (per product decision): the request contract in 7.1 and authentication are done.

---

## 2. Architecture and request flow

```
Controller (Phase7TestController: POST /api/phase7-test/process)
  → Phase7WorkflowService.ProcessAsync(Phase7WorkflowRequestDto)
      → IMicrosoft365IntegrationService   (real: Microsoft365IntegrationService | mock: MockMicrosoft365IntegrationService)
      → Microsoft365LicenseStatusValidator (pure logic)
      → MailboxCapacityAnalyzer            (pure logic)
  → Phase7WorkflowResultDto { RequestId, UseCase, Success, Status, Message, IsMock }

`IExchangeIntegrationService` (Windows PowerShell + certificate) is used only by `GET /exchange`.
The workflow no longer depends on it.
```

Real vs mock is chosen at startup in `Resolve-AI.API/Program.cs`: if `AZURE_CLIENT_SECRET` is set,
the real service is registered, otherwise the mock. `.env` is loaded with `../.env` relative to the
working directory, so run the API from `Resolve-AI.API/Resolve-AI.API/`. If `.env` is not found the app
silently falls back to the mock.

**Mock is outdated:** its license status has no `AssignedSkuIds`, so it never reports Business Premium.
Mock UC-07 assignment therefore fails the 7.8 verification by design. Mock is not a valid test path.

---

## 3. WBS steps (current code)

Step numbers follow the WBS list used in planning. Code comments use the same numbers, except the
Exchange connection check, which is a precondition for UC-08 and not a WBS step of its own.

| WBS | Work | Code | Tested on real tenant |
|---|---|---|---|
| 7.1 | Request intake (contract assumed final) | Test endpoint `POST /api/phase7-test/process` | Yes |
| 7.2 | Request validation | `Phase7WorkflowService` (7.03 in comments) | Yes (`RequestId`, use case) |
| 7.3 | Microsoft 365 availability | `CheckAvailabilityAsync` | Yes (`True`) |
| 7.4 | User lookup (404 = not found, `accountEnabled` must be true) | `GetUserAsync` (selects `accountEnabled`, `usageLocation`) | Yes (found, not found, disabled: `USER_DISABLED`) |
| 7.5 | License status (paged, Business Premium + Teams) | `GetLicenseStatusAsync` | Yes (already-licensed case) |
| 7.6 | License validation | `Microsoft365LicenseStatusValidator` | Yes (already-licensed case) |
| 7.7 | Approved license assignment (approval, usage location, seat and SKU guards) | `AssignLicenseAsync` + workflow | **Yes** on `rai.test01` (SPB assigned, Teams `Success`, confirmed in Graph). Usage-location and seat-full paths: code only |
| 7.8 | Assignment verification (re-read up to 5 times, 5 s apart); unverified → `ASSIGNED_UNVERIFIED` | `WaitForLicenseAsync` in workflow | **Yes** on `rai.test01` (verified). Unverified path: code only |
| 7.9 | Mailbox usage (Graph usage report `D7`, `Storage Used` vs `Prohibit Send/Receive Quota`, `Is Deleted` rows skipped) | `GetMailboxUsageAsync` | **Yes**: Hassan row found. `Is Deleted` filter: code only |
| 7.10 | Capacity analysis (NORMAL < 80%, WARNING ≥ 80%, CRITICAL ≥ 90%, UNKNOWN if no quota) | `MailboxCapacityAnalyzer` | **Yes**: UC-08 returns `NORMAL`, 0.0% (correct for this mailbox) |

Exchange (not a workflow precondition any more): `ExchangeIntegrationService` (Windows PowerShell 5.1, `ExchangeOnlineManagement` 3.10.1).
`GET /api/phase7-test/exchange` returns `HTTP 200, isAvailable: true`.

UC-08 real result (2026-10-12): `success=True`, "Mailbox usage is 0.0% of quota (NORMAL)." Raw report row checked and confirmed.

### Outcome status codes (`Phase7Status`, in `result.Status`)

| Status | Success | Meaning |
|---|---|---|
| `COMPLETED` | true | License assigned and verified (Business Premium with active Teams) |
| `NO_ACTION_REQUIRED` | true | User already has the license with Teams active |
| `CAPACITY_ANALYSED` | true | UC-08 analysed (state NORMAL, WARNING or CRITICAL in message) |
| `CAPACITY_UNKNOWN` | false | UC-08: no quota available |
| `ASSIGNED_UNVERIFIED` | false | Graph accepted the assignment but the license was not visible after 5 checks. Check manually before any retry |
| `NOT_APPROVED` | false | Approval is false, nothing assigned |
| `USER_NOT_FOUND` | false | Graph 404 on user lookup |
| `USER_DISABLED` | false | `accountEnabled` is false |
| `USAGE_LOCATION_MISSING` | false | Approved, but the user has no usage location (Graph needs one before assignment) |
| `LICENSE_REVIEW_REQUIRED` | false | Business Premium present but Teams not active |
| `NO_FREE_SEATS`, `SKU_NOT_AVAILABLE`, `SKU_NOT_ENABLED`, `TEAMS_NOT_AVAILABLE`, `TEAMS_REVIEW_REQUIRED` | false | Assignment refused by the tenant checks (pass-through from `AssignmentStatus`) |
| `FAILED` | false | Assignment call failed (pass-through) |
| `MAILBOX_DATA_NOT_FOUND` | false | UC-08: no usage row for the user |
| `M365_UNAVAILABLE` | false | Microsoft 365 availability check failed (Graph error or not reachable) |
| `VALIDATION_FAILED` | false | Request validation failed (RequestId, UserIdentifier, UseCase) |
| `WORKFLOW_ERROR` | false | Unexpected error in a Microsoft 365 call |

The UC-08 Exchange precondition was removed; UC-08 no longer depends on PowerShell.

### Outcomes

**UC-07**
- Validation, availability, lookup or disabled-account failure → `Success = false` with a status code.
- Required license and Teams already active → `NO_ACTION_REQUIRED`, `Success = true` (no assignment).
- Required license but Teams not active → `LICENSE_REVIEW_REQUIRED`.
- Not approved → `NOT_APPROVED`, nothing assigned.
- Approved but no usage location → `USAGE_LOCATION_MISSING`, nothing assigned.
- Approved and assignable → `assignLicense` is called. Then 7.8 re-reads the license. `COMPLETED` only when Business Premium and Teams are visible. Otherwise `ASSIGNED_UNVERIFIED`.

**UC-08**
- Usage not found → `MAILBOX_DATA_NOT_FOUND`.
- Otherwise → `CAPACITY_ANALYSED` when state is NORMAL, WARNING or CRITICAL. `CAPACITY_UNKNOWN` only when quota is unknown.

`IsMock` is taken from the integration DTOs that answered (not hardcoded in workflow).

---

## 4. Configuration (names only, never commit values)

`Resolve-AI.API/.env` (git-ignored):

| Variable | Used by | Current value |
|---|---|---|
| `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` | Microsoft Graph (client credentials) | set |
| `POWERSHELL_PATH` | Shell for Exchange | `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe` (5.1, has the module) |
| `EXCHANGE_APP_ID`, `EXCHANGE_ORGANIZATION`, `EXCHANGE_CERTIFICATE_THUMBPRINT` | Exchange Online app-only auth | set (certificate in CurrentUser store) |

Azure app permissions: `Directory.Read.All`, `Directory.ReadWrite.All`, `Reports.Read.All` (all granted, admin consent done).
Tenant setting: "Display concealed user, group, and site names in all reports" is **off** (done by admin). Without this, the usage report hashes user names and 7.9 cannot match a user.

**Security action still open:** the Azure client secret was shown in plain text in an earlier chat. Rotate it in Azure Portal and update `.env`.

---

## 5. Test endpoints (`/api/phase7-test`, port 5084)

| Method and path | Purpose | Safe? |
|---|---|---|
| `GET /microsoft365` | Availability (`isMock` text in response is hardcoded, ignore it) | Yes |
| `GET /microsoft365-users` | Users list (per-user `isMock` is accurate) | Yes |
| `POST /process` | Full workflow | Only read-only cases unless `isApproved = true` for an unlicensed user (**assigns a paid seat**) |
| `GET /exchange` | Exchange precondition | Yes |
| `GET /license-skus` | Tenant SKUs, seats, service plans | Yes |
| `POST /microsoft365-license-assign` | Real `AssignLicenseAsync` with approval | **No** (paid seat) |

Read-only flow on 2026-10-12 (all passed):
1. Availability → `True`
2. Users list → Hassan Khan, `isMock=False`
3. UC-07 Hassan (already licensed) → `success=True`, no assignment
4. UC-07 unknown user → `success=False`, not found
5. `/exchange` → `HTTP 200`, `isAvailable: true`
6. UC-08 Hassan → `success=True`, `NORMAL`, 0.0%

Earlier blockers, now resolved (kept for history):
- `POWERSHELL_PATH` pointed to pwsh 7.6.6 without the Exchange module → changed to Windows PowerShell 5.1.
- `Reports.Read.All` missing → granted, admin consent done.
- Report user names were concealed → setting turned off by admin.

---

## 6. Git state

Pushed to `origin/shahbaz/phase7-license-mailbox`:
- `ea99132` Real Microsoft Graph integration for 7.3 to 7.7
- `9acbdcf` Fixes: already-licensed case, lookup errors, SKU selection, `isMock` flag

**Uncommitted on disk** (not committed or pushed):
- Modified: `Program.cs`, `Phase7WorkflowService.cs`, `Phase7WorkflowResultDto.cs`, `Microsoft365UserDto.cs`, `Microsoft365IntegrationService.cs`, `MockMicrosoft365IntegrationService.cs`, `IMicrosoft365IntegrationService.cs`, `Microsoft365LicenseStatusDto.cs`, `Microsoft365LicenseStatusValidator.cs`
- New: `Phase7Status.cs`, `ExchangeIntegrationService.cs`, `IExchangeIntegrationService.cs`, `ExchangeTestController.cs`, `LicenseSkuTestController.cs`, `MailboxUsageDto.cs`, `MailboxCapacityResultDto.cs`, `MailboxCapacityAnalyzer.cs`, `CLAUDE.md`, `PHASE7_STATUS.md`, `Resolve-AI-Exchange.cer`
- Clutter to remove or ignore before commit: `*.bak` (4 files), `deck/`, `deck2/`, `outputs/`, `presentation.md`, `.claude/`

---

## 7. Resolved incident: API would not start (Windows Smart App Control)

Symptom: `FileLoadException ... An Application Control policy has blocked this file (0x800711C7)`.

Verified (2026-10-08 to 10-10):
- Cause: Windows **Smart App Control**, on at the time. Events 3077/3033/3118 in `Microsoft-Windows-CodeIntegrity/Operational`. Reason: signing level requested 2, validated 1.
- The block followed one specific file hash. Path, drive and project did not matter. Many fresh-hash builds loaded fine.
- Unchanged rebuilds keep the same hash (deterministic), so blocks repeat for the same build.
- Smart App Control was switched **off** by the user on 2026-10-12. After that, the same DLL loaded and the API ran.

Not known: why that first hash was blocked and why earlier builds passed.

---

## 8. Verification status

**Verified on the real tenant** (2026-10-13, current working tree, API on 5084):
- Availability, users list, already-licensed UC-07 (`NO_ACTION_REQUIRED`), not-approved UC-07 (`NOT_APPROVED`), unknown user (`USER_NOT_FOUND`), bad use case (`VALIDATION_FAILED`), UC-08 Hassan (`CAPACITY_ANALYSED`), `/exchange`.
- Disabled account (temporary disable of `rai.test02`): `USER_DISABLED`, then re-enabled.
- 7.7 and 7.8: `rai.test01` (test user) assigned Business Premium, Teams `Success`, confirmed directly in Graph. Repeat run returns `NO_ACTION_REQUIRED` (no second assignment).
- Bug found and fixed in this session: Graph does not return `accountEnabled` or `usageLocation` unless they are selected. Without that, every user looked disabled.
- Report column names checked against the real report header.
- Build: `0 Error(s)`.

**Not verified yet:**
- `USAGE_LOCATION_MISSING` path (needs a test user without usage location).
- `ASSIGNED_UNVERIFIED` path (needs Graph to accept an assignment and then not show it).
- `NO_FREE_SEATS` path (tenant has 23 free seats; would need full seats to test).
- `Is Deleted` row filter (no deleted mailbox in test data).
- Capacity thresholds (80% / 90%) against a mailbox near its limit.
- UC-07 with `TenantIdentifier` (ignored, one tenant from `.env`).

**Test data created in the real tenant (2026-10-13):**
- `rai.test01@SystemCastle1.onmicrosoft.com`: Business Premium assigned (paid seat in use). Delete or keep, your decision.
- `rai.test02@SystemCastle1.onmicrosoft.com`: unlicensed, enabled.

---

## 9. Known issues and risks

1. **No API authentication.** Caller auth is assumed done (product decision), but there is no `[Authorize]` in code. Test controllers expose tenant data and can assign licenses.
2. **Required SKU ID hardcoded in three places** (service, validator, DTO). Keep in one place.
3. **Capacity thresholds (80% / 90%) are provisional.** Business must confirm.
4. **Graph usage report is delayed** (often about a day). Usage may be stale; a mailbox created today may show as not found.
5. **Mock path is outdated** (see section 2).
6. **`LicenseSkuTestController` bypasses DI** and builds its own Graph client.
7. **Step numbering in old class comments** (7.2 to 7.7) may still exist in other files.
8. **Silent mock fallback** if `.env` is not found.
9. **Build warnings:** `Microsoft.OpenApi 2.0.0` known high severity (NU1903); `DotNetEnv` 3.1.2 resolved to 3.2.0 (NU1603).
10. **Client secret rotation** still pending.
11. **Graph `$select` rule:** `accountEnabled` and `usageLocation` only come back when selected. Any new user lookup must select them.
12. **Assignment has no concurrency guard.** Two parallel approved requests for the same user can race.
13. **Conflicting plans** (for example E3 or E5 already assigned) are not handled before assignment.
14. **Usage location is required** before assignment. Missing usage location returns `USAGE_LOCATION_MISSING`, but nothing sets it automatically.

---

## 10. Next steps

1. **Business:** confirm capacity thresholds (80% / 90%).
2. **Test data:** decide whether to keep or delete `rai.test01` (paid seat) and `rai.test02`.
3. **Negative paths:** test `USAGE_LOCATION_MISSING` and `ASSIGNED_UNVERIFIED` with test data (see section 8).
4. Clean the working tree (remove `*.bak`, decide on `deck/`, `deck2/`, `outputs/`, `presentation.md`, `.claude/`), then commit and push.
5. Rotate the client secret.
6. Build 7.11 to 7.14 (quota evaluation, alerts, notifications, monitoring).
7. Build 7.15 to 7.17 (result compilation, orchestration handoff, exit) once the contract is confirmed.
