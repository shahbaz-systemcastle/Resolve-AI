# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Run from the solution folder (`Resolve-AI.API/`, the one containing `Resolve-AI.API.slnx`). Note the git root is one level up (`D:\Resolve AI`), so git paths are prefixed with `Resolve-AI.API/`.

- Build: `dotnet build Resolve-AI.API.slnx`
- Run: `dotnet run --project Resolve-AI.API/Resolve-AI.API.csproj` (http://localhost:5084; https profile also uses 7190)
- Manual API calls: `Resolve-AI.API/Resolve-AI.API.http` and the `/api/phase7-test/*` endpoints

There are no test projects, no lint config, and no CI workflows. Verification is done by running the API and calling endpoints.

## Architecture

Layered, targeting .NET 10: `resolve-AI.Domain` (enums such as `UseCaseType`) ← `resolve-AI.Application` (DTOs, interfaces, workflows, validation) ← `resolve-AI.Infrastructure` (implementations of the Application interfaces). `Resolve-AI.API` references Application and Infrastructure and does all DI wiring in `Program.cs`. Folder and project names use `resolve-AI.*` while namespaces use `resolve_AI.*`.

Two independent request flows exist:

- **Support flow** (`POST /api/requests`): `SupportController` → `SupportRequestService` matches keywords in the message (only UC-01 password expired) → `PasswordExpiredWorkflow` → `MockPasswordService`.
- **Phase 7 flow** (License and Mailbox Management, UC-07 and UC-08): `Phase7WorkflowService.ProcessAsync` is the orchestrator. It validates the request, then calls `IMicrosoft365IntegrationService` (availability, user lookup, license status, license assignment) with `Microsoft365LicenseStatusValidator` deciding whether an assignment is needed. `IExchangeIntegrationService` is only used by the `/exchange` test endpoint, not by the workflow. Only use cases 7 and 8 are accepted; all others fail validation. UC-07 runs 7.03 to 7.08 and then re-reads the license (7.8, `WaitForLicenseAsync`) before reporting success. UC-08 reads mailbox usage from the Graph usage report (7.9, `GetMailboxUsageAsync`) and classifies it (7.10, `MailboxCapacityAnalyzer`, thresholds 80/90 percent, provisional). Current status, step numbering and known issues are in `PHASE7_STATUS.md`.

### Real vs mock switching (easy to get wrong)

`Program.cs` loads `../.env` with DotNetEnv and registers the real `Microsoft365IntegrationService` (Microsoft Graph, client credentials) only if `AZURE_CLIENT_SECRET` is set; otherwise `MockMicrosoft365IntegrationService`. The `.env` path is relative to the working directory: `dotnet run` uses the project directory, so `.env` belongs in the solution folder. If it is not found, the app silently falls back to the mock. `IsMock` in results comes from the integration DTOs (trust it); the `IsMock = true` text in `GET /api/phase7-test/microsoft365` is hardcoded and wrong.

Env vars: `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` (Graph) and `POWERSHELL_PATH`, `EXCHANGE_APP_ID`, `EXCHANGE_ORGANIZATION`, `EXCHANGE_CERTIFICATE_THUMBPRINT` (Exchange).

### Non-obvious details

- `ExchangeIntegrationService` does not use an SDK. It starts a PowerShell process, imports `ExchangeOnlineManagement`, and connects with certificate auth, so it needs the module and certificate installed on the host.
- The required license SKU (Microsoft 365 Business Premium plus an active Teams plan) is hardcoded in three places: `Microsoft365IntegrationService`, `Microsoft365LicenseStatusValidator`, `Microsoft365LicenseStatusDto`. Change all three together.
- In the real service, Graph errors propagate to the workflow, which converts them into a failed result. Only a 404 on user lookup means "not found".
- `Phase7TestController` and `LicenseSkuTestController` are unauthenticated and call the real tenant. `POST /api/phase7-test/process` with `isApproved = true` for an unlicensed user, and `POST /api/phase7-test/microsoft365-license-assign`, perform real license assignments.
- On a machine with Windows Smart App Control on, a freshly built `resolve-AI.Application.dll` can be blocked (`0x800711C7`) and the API will not start. Details are in `PHASE7_STATUS.md`.
