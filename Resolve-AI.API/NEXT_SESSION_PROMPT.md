# Next Session Prompt: Resolve-AI Phase 7

You are continuing work on the Resolve-AI project (ASP.NET Core, .NET 10) for the Phase 7 License and Mailbox Management module. Read this fully before acting.

## Project

- Solution folder: `D:\Resolve AI\Resolve-AI.API` (contains `Resolve-AI.API.slnx` and `.env`, which is git-ignored)
- Git root: `D:\Resolve AI`. Git paths are prefixed with `Resolve-AI.API/`.
- Branch: `shahbaz/phase7-license-mailbox`
- Layers: `resolve-AI.Domain` <- `resolve-AI.Application` <- `resolve-AI.Infrastructure`. `Resolve-AI.API` does DI in `Program.cs`.
- Read `Resolve-AI.API/CLAUDE.md` and `Resolve-AI.API/PHASE7_STATUS.md` first.

## Architecture (Phase 7)

- Orchestrator: `Phase7WorkflowService.ProcessAsync`
- Microsoft 365 calls go through `IMicrosoft365IntegrationService`.
  - Real: `Microsoft365IntegrationService` (Microsoft Graph, client credentials via Azure.Identity). Used when `AZURE_CLIENT_SECRET` is set.
  - No mock. The mock service was removed on 2026-10-27; without `AZURE_*` values the Graph service throws.
- Exchange: `ExchangeIntegrationService` (PowerShell + ExchangeOnlineManagement). Used only by `GET /api/phase7-test/exchange`, NOT by the workflow.
- Required SKU (Business Premium) `cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46` with Teams plan `TEAMS1` must be provisioned `Success`. Hardcoded in 3 places: `Microsoft365IntegrationService`, `Microsoft365LicenseStatusValidator`, `Microsoft365LicenseStatusDto`.
- Status codes are constants in `resolve_AI.Application.DTOs.Phase7Status`.

## WBS mapping (user numbering -> code numbering)

- 7.1 request format: final, contact and auth done. 7.2 ignored by user.
- 7.3 -> 7.04 Microsoft 365 availability (Graph `GET /organization`)
- 7.4 -> 7.05 user lookup (Graph `GET /users/{upn}` with `$select`; 404 = not found; disabled account and missing usageLocation handled)
- 7.5 -> 7.06 license status (`licenseDetails`, paged)
- 7.6 -> 7.07 validation, approval check, usage location check
- 7.7 -> 7.07 assignment (`subscribedSkus` seat/SKU checks, then `assignLicense` POST)
- 7.8 -> 7.08 verification: re-read license up to 5 times, 5 s apart; else `ASSIGNED_UNVERIFIED`
- 7.9 -> mailbox usage from Graph `reports/getMailboxUsageDetail(D7)` CSV (UC-08)
- 7.10 -> `MailboxCapacityAnalyzer`: WARNING 80%, CRITICAL 90% (PROVISIONAL, business to confirm)
- 7.11 -> `QuotaEvaluator` (pure logic): WARNING/CRITICAL = `ALERT_REQUIRED`; NORMAL = `NO_ALERT`; UNKNOWN = `NOT_EVALUATED`. Result in `Phase7WorkflowResultDto.QuotaEvaluation`. Does NOT set up alerts or notify.
- 7.12 -> `MailboxAlertSetup` (pure logic): when 7.11 says `ALERT_REQUIRED`, creates an OPEN `MailboxCapacityAlertDto` (AlertId `MBX-{user}-{severity}-{yyyyMMdd}`, threshold 80 or 90). Result in `Phase7WorkflowResultDto.Alert`. Creates a record only; sends nothing. This reading of 7.12 was chosen by the assistant because no WBS text was given; confirm with the user.
- 7.13 -> `IMailboxAlertNotifier` (Application) with `LoggingMailboxAlertNotifier` (Infrastructure, registered in `Program.cs`). Default only logs, status `NOT_SENT`, channel `LOG`. Result in `Phase7WorkflowResultDto.AlertNotification`. Real delivery (email or Teams) is NOT built: channel and recipients are pending user decision.
- 7.14 (monitoring) and 7.15 to 7.16 (result compilation, orchestration handoff): NOT STARTED. Need WBS text from the user.

## Current state

- Commit `85b5dfd` on the branch: Phase 7 UC-07/UC-08, Exchange check, cleanup. NOT pushed. The user said do NOT push unless asked.
- Commit `8c8e882`: 7.11 (quota evaluation). Committed on user's approval. NOT pushed.
- 7.12 and 7.13 code is UNCOMMITTED: `DTOs/MailboxCapacityAlertDto.cs`, `DTOs/MailboxAlertNotificationResultDto.cs`, `Validation/MailboxAlertSetup.cs`, `Interfaces/IMailboxAlertNotifier.cs`, `Infrastructure/Services/LoggingMailboxAlertNotifier.cs`, and changes to `Phase7WorkflowResultDto.cs`, `Phase7WorkflowService.cs`, `Resolve-AI.API/Program.cs`.
- Untracked, do NOT commit unless asked: `deck/`, `deck2/`, `outputs/`, `presentation.md`, `NEXT_SESSION_PROMPT.md`.
- Verified:
  - Build passes (0 errors) after 7.11, 7.12 and 7.13.
  - Live tenant UC-08 for `HassanKhan@SystemCastle1.onmicrosoft.com`: usage 0%, NORMAL, decision `NO_ALERT`, alert null, notification null, `isMock` false (after 7.13 build).
  - Simulation moved into the repo: `tools/Phase7Simulation` (run with `dotnet run` from that folder). Covers 7.13 to 7.15 with fake data; all cases pass as of 2026-10-27.
  - Live check: `scripts/phase7-live-check.ps1` (14 of 14 pass on 2026-10-27).
  - (Older) `QuotaEvaluator` console harness: NORMAL -> NO_ALERT, WARNING -> ALERT_REQUIRED, CRITICAL -> ALERT_REQUIRED, UNKNOWN -> NOT_EVALUATED.
  - `MailboxAlertSetup` console harness: NORMAL and UNKNOWN -> no alert; WARNING -> `MBX-u@x-WARNING-20261023`, threshold 80, OPEN; CRITICAL -> threshold 90, OPEN.
  - UC-07 earlier: real assignment and verification on the tenant worked (`rai.test01` got Business Premium).
  - `GET /exchange` returns `isAvailable: true`, `isMock: false`.
- NOT verified live: WARNING/CRITICAL end-to-end through the API (no suitable test user), so the notifier path is not run live. `USAGE_LOCATION_MISSING`, `ASSIGNED_UNVERIFIED`, `NO_FREE_SEATS` also not verified live.
- `PHASE7_STATUS.md` was updated on 2026-10-24 and now covers 7.11, 7.12 and 7.13 (test results and what is still unverified).
- Branch is 2 commits ahead of origin (`85b5dfd`, `8c8e882`). Not pushed.

## Tenant and permissions

- Tenant `SystemCastle1.onmicrosoft.com` currently has one user: `HassanKhan@SystemCastle1.onmicrosoft.com` (enabled, mailbox 0%).
- `rai.test01` and `rai.test02` were deleted by the user's team lead (manual, via Entra portal). The app cannot delete users: it returns 403 (no `User.ReadWrite.All`).
- The app cannot create users either. Do not try to change app permissions; that is the user's decision.
- The client secret was exposed in chat earlier. The user must rotate it in the Azure Portal (user action).

## Ports and processes

- The old API process (pid 23592) was stopped by the user. Use port 5085 for test runs.
- Build output in `bin/` gets locked by a running API. If the build fails with MSB3027 or MSB3021, stop the running API first.
- The user runs Windows PowerShell 5.1 (Visual Studio Developer PowerShell). Do not use `&&` in commands for them. Use `Invoke-RestMethod` for JSON POST; `curl.exe` quoting breaks in PS 5.1.

## Rules from the user (follow strictly)

- Scope: only work on what the user asks. The user asked for 7.11, 7.12 and 7.13; these are done in code. Do not start 7.14 without the WBS text and approval.
- Notification (7.13): do not send any real email or Teams message. Real delivery needs the channel (email, Teams, or both) and the recipients confirmed by the user, plus approval for each test send.
- Ask before code changes when the user asks to discuss first.
- Do not commit or push unless the user explicitly asks.
- Do not create `.bak` files; use git.
- Never print or store secret values (`.env` contents, client secret, tokens) in chat or files.
- Use one language per reply where possible. The user writes in Urdu/English mix; avoid unclear mixing. Use they/them for people unless pronouns are given.
- Outward-facing or irreversible actions (sending notifications, deleting users, paid license assignment, pushing) need explicit approval in chat.
- Never delete files without showing the user the list and getting approval first. Deleted untracked files cannot be recovered with git.
- Report outcomes faithfully. If a test was not run live, say so.
- Code style: match existing comments (WBS step tags like `// 7.10 - ...`), no unnecessary comments, MOCK/TBD comments only where still unconfirmed.

## Open items

1. Commit 7.12 and 7.13 only if the user asks (`PHASE7_STATUS.md` already updated).
2. Commit 7.12 and 7.13 only if the user asks. List files explicitly and exclude `deck/`, `deck2/`, `outputs/`, `presentation.md`, `NEXT_SESSION_PROMPT.md`.
3. Business to confirm the 80%/90% thresholds.
4. Notification channel and recipients (for 7.13) need the user's decision.
5. WBS 7.14 to 7.16 text needed before any work on them.
5. `LicenseSkuTestController` and the mock endpoints were removed on 2026-10-27. Remaining: `Phase7WorkflowRequestMock` still uses the fake user `test.user@mock.local` for `/uc07` and `/uc08`.
6. Move the SKU ID to one shared constant (currently in three places).
7. Known warnings: Microsoft.OpenApi 2.0.0 (NU1903); DotNetEnv 3.2.0 resolved for 3.1.2 (NU1603).

## First steps

1. Read `CLAUDE.md`, `PHASE7_STATUS.md`, and `git status`.
2. Ask the user what they want next: update `PHASE7_STATUS.md`, commit 7.12 and 7.13, decide the notification channel, or something else. Do not start 7.14 without the WBS text.

## Unused files review (2026-10-13, nothing deleted yet)

- C# code: no unused `.cs` files found. Every class is referenced by another file, or is a controller (reached by route).
- Non-code items outside the project, all untracked, none related to Phase 7. Delete only after user approval (git cannot restore untracked files):

| Path | Contents | Size |
|---|---|---|
| `deck/` | Presentation project (`project/deck.json`, `project/slides/*.html`) | 17 files, 72 KB |
| `deck2/` | Second presentation project | 15 files, 64 KB |
| `outputs/ai-meeting-api-pricing/` | Pricing output | 1 file, 12 KB |
| `presentation.md` | Presentation text | 7.5 KB |

- Keep: `Resolve-AI-Exchange.cer` (Exchange certificate, needed for setup, documented in `PHASE7_STATUS.md`).
- Already removed earlier: `WeatherForecast.cs`, `WeatherForecastController.cs`, `.bak` files, `archive-H2VSI4/gk_3.1.75_windows_amd64.zip`.
