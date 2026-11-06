# Phase 4: Automation Orchestration Module
## Architecture Design (Approach 1: Sequential Routing)

---

## Overview

**Purpose:** Route automation requests from Phase 3 to appropriate execution phases (5, 6, or 7) and collect results for Phase 8.

**Input:** Structured automation request from Phase 3
**Output:** Execution result to Phase 8

```
Phase 3 (AI Agent)
    ↓
Phase 4 (Orchestrator) ← You are here
├─ 4.01: Receive request
├─ 4.02: Validate
├─ 4.03: Determine routing
├─ 4.04-4.06: Execute Phase 5/6/7
├─ 4.07-4.10: Collect results
├─ 4.11: Error handling
└─ 4.12: Pass to Phase 8
    ↓
Phase 8 (Logging)
```

---

## Architecture Structure

### 1. Domain Models

**Location:** `resolve-AI.Domain/Entities/Phase4/`

```
AutomationRequest.cs
├─ Id
├─ UserId
├─ UseCase (UC-01...UC-13)
├─ RequestData
├─ Status
└─ CreatedAt

AutomationRoute.cs
├─ RequestId
├─ TargetPhase (5, 6, or 7)
├─ Reason
└─ Priority

ExecutionResult.cs
├─ RequestId
├─ PhaseNumber
├─ Status (Success, Failed, Partial)
├─ Data
├─ Error
└─ ExecutedAt
```

---

### 2. Service Interfaces

**Location:** `resolve-AI.Application/Interfaces/`

```csharp
// 4.01-4.03: Request Reception & Routing
IPhase4OrchestrationService
├─ ReceiveRequest(request)
├─ ValidateRequest(request)
└─ DetermineRoute(request) → Route

// 4.04-4.06: Phase Execution
IPhase5ExecutionProxy
├─ ExecuteAsync(request)

IPhase6ExecutionProxy
├─ ExecuteAsync(request)

IPhase7ExecutionProxy
├─ ExecuteAsync(request)

// 4.07-4.10: Result Collection
IPhase4ResultCollector
├─ CollectResult(phaseNumber, result)
├─ AggregateResults()
└─ PrepareForPhase8()

// 4.11: Error Handling
IPhase4ErrorHandler
├─ HandlePhaseError(error, context)
└─ RollbackIfNeeded(context)
```

---

### 3. Controller

**Location:** `Resolve-AI.API/Controllers/Phase4Controller.cs`

```csharp
[ApiController]
[Route("api/phase4")]
public class Phase4Controller
{
    // 4.01: Receive request from Phase 3
    [HttpPost("process")]
    public async Task<IActionResult> ProcessRequest(AutomationRequest request)
    
    // 4.02: Validate request
    private IActionResult ValidateRequest(AutomationRequest request)
    
    // 4.03: Determine routing
    [HttpPost("determine-route")]
    public IActionResult DetermineRoute(AutomationRequest request)
    
    // Status check
    [HttpGet("status/{requestId}")]
    public async Task<IActionResult> GetStatus(string requestId)
}
```

---

## WBS Breakdown (4.01-4.12)

| WBS | Task | Responsibility | Status |
|-----|------|-----------------|--------|
| 4.01 | Request Reception | Phase4Controller receives from Phase 3 | - |
| 4.02 | Request Validation | Validate schema, required fields | - |
| 4.03 | Route Determination | Determine target phase (5/6/7) | - |
| 4.04 | Phase 5 Execution | Call Phase 5 (Device/AD) if needed | - |
| 4.05 | Phase 6 Execution | Call Phase 6 (MFA) if needed | - |
| 4.06 | Phase 7 Execution | Call Phase 7 (License/Mailbox) if needed | - |
| 4.07 | Result Collection | Collect individual phase results | - |
| 4.08 | Result Aggregation | Merge Phase 5/6/7 results | - |
| 4.09 | Result Validation | Validate combined result | - |
| 4.10 | Result Preparation | Format for Phase 8 | - |
| 4.11 | Error Handling | Handle phase-level errors | - |
| 4.12 | Phase 8 Handoff | Pass result to Phase 8 | - |

---

## Data Flow

```
1. Phase 3 sends:
   {
     "requestId": "req-123",
     "userId": "user@org.com",
     "useCase": "UC-05",
     "actionType": "reset_password",
     "metadata": {...}
   }

2. Phase 4 validates & routes:
   - Is useCase valid? (UC-01 to UC-13)
   - Which phase should handle it?
   - UC-01-03: Phase 5 (Device)
   - UC-04-06: Phase 6 (MFA)
   - UC-07-08: Phase 7 (License)
   - UC-09-13: Phase 5/6 (Mixed)

3. Phase 4 executes:
   - Call appropriate phase service
   - Wait for result
   - Handle errors

4. Phase 4 aggregates:
   {
     "requestId": "req-123",
     "status": "success",
     "phaseResults": [
       { "phase": 5, "status": "success", "data": {...} },
       { "phase": 6, "status": "success", "data": {...} }
     ],
     "finalStatus": "completed"
   }

5. Phase 4 sends to Phase 8
```

---

## Routing Logic (4.03)

```
switch(useCase) {
  case UC-01-03:  → Phase 5 (Device/AD)
  case UC-04-06:  → Phase 6 (MFA)
  case UC-07-08:  → Phase 7 (License/Mailbox)
  case UC-09-10:  → Phase 5 + Phase 6 (Parallel)
  case UC-11-12:  → Phase 5 + Phase 7 (Sequential)
  case UC-13:     → Phase 5 + Phase 6 + Phase 7 (Sequential)
  default:        → Error
}
```

---

## File Structure

```
Resolve-AI.Domain/
├─ Entities/
│  └─ Phase4/
│     ├─ AutomationRequest.cs
│     ├─ AutomationRoute.cs
│     └─ ExecutionResult.cs

resolve-AI.Application/
├─ Interfaces/
│  ├─ IPhase4OrchestrationService.cs
│  ├─ IPhase5ExecutionProxy.cs
│  ├─ IPhase6ExecutionProxy.cs
│  ├─ IPhase7ExecutionProxy.cs
│  ├─ IPhase4ResultCollector.cs
│  └─ IPhase4ErrorHandler.cs
├─ Services/
│  └─ Phase4/
│     ├─ Phase4OrchestrationService.cs
│     ├─ Phase4RoutingResolver.cs
│     ├─ Phase4ResultCollector.cs
│     └─ Phase4ErrorHandler.cs

resolve-AI.Infrastructure/
├─ Services/
│  └─ Phase4/
│     ├─ Phase5ExecutionProxy.cs
│     ├─ Phase6ExecutionProxy.cs
│     └─ Phase7ExecutionProxy.cs

Resolve-AI.API/
├─ Controllers/
│  └─ Phase4Controller.cs
├─ Program.cs (register services)
```

---

## Interface Contracts

### IPhase4OrchestrationService

```csharp
public interface IPhase4OrchestrationService
{
    Task<OrchestrationResult> ProcessAsync(
        AutomationRequest request,
        CancellationToken cancellationToken);
    
    Task<AutomationRoute> DetermineRouteAsync(
        AutomationRequest request);
}
```

### IPhase5ExecutionProxy

```csharp
public interface IPhase5ExecutionProxy
{
    Task<Phase5Result> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken);
}
```

### IPhase6ExecutionProxy

```csharp
public interface IPhase6ExecutionProxy
{
    Task<Phase6Result> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken);
}
```

### IPhase7ExecutionProxy

```csharp
public interface IPhase7ExecutionProxy
{
    Task<Phase7Result> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken);
}
```

### IPhase4ResultCollector

```csharp
public interface IPhase4ResultCollector
{
    void AddResult(int phaseNumber, ExecutionResult result);
    ExecutionResult AggregateResults();
}
```

### IPhase4ErrorHandler

```csharp
public interface IPhase4ErrorHandler
{
    Task<ErrorHandlingResult> HandlePhaseErrorAsync(
        Exception error,
        OrchestrationContext context);
}
```

---

## DI Registration (Program.cs)

```csharp
// Phase 4 Services
builder.Services.AddScoped<IPhase4OrchestrationService, 
    Phase4OrchestrationService>();
builder.Services.AddScoped<IPhase4ResultCollector, 
    Phase4ResultCollector>();
builder.Services.AddScoped<IPhase4ErrorHandler, 
    Phase4ErrorHandler>();

// Execution Proxies (call to Phases 5, 6, 7)
builder.Services.AddScoped<IPhase5ExecutionProxy, 
    Phase5ExecutionProxy>();
builder.Services.AddScoped<IPhase6ExecutionProxy, 
    Phase6ExecutionProxy>();
builder.Services.AddScoped<IPhase7ExecutionProxy, 
    Phase7ExecutionProxy>();
```

---

## Notes

- **Sequential Execution:** Phase 4.04-4.06 execute one at a time (not parallel)
- **Error Propagation:** If one phase fails, 4.11 decides: continue or stop
- **Timeout:** Each phase execution has timeout (e.g., 30 seconds)
- **Logging:** All orchestration steps logged for audit trail
- **Idempotent:** Requests with same ID should produce same result

---

**Next Step:** Khalil implements 4.01-4.12 using this architecture.
