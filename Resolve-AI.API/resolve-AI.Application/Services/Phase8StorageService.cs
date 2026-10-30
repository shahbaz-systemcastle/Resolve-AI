using resolve_AI.Application.Interfaces;
using resolve_AI.Domain.Entities;

namespace resolve_AI.Application.Services;

/// <summary>
/// Phase 8 - Result and Logging Module Implementation
/// WBS 8.12-8.22: Receives Phase 7 outcomes, stores request/result/audit records.
///
/// Current implementation: In-memory storage (for testing).
/// Future: Replace with SQL Server / Entity Framework.
/// </summary>
public class Phase8StorageService : IPhase8StorageService
{
    // In-memory storage (TODO: Replace with database)
    private static readonly List<Phase8Request> Requests = new();
    private static readonly List<Phase8Result> Results = new();
    private static readonly List<Phase8AuditLog> AuditLogs = new();
    private static int _requestIdCounter = 1;
    private static int _resultIdCounter = 1;
    private static int _auditIdCounter = 1;

    public Phase8StorageService()
    {
    }

    /// <summary>
    /// WBS 8.17 - Store the incoming request.
    /// </summary>
    public async Task<int> SaveRequestAsync(
        string requestId,
        string userIdentifier,
        int useCase,
        string tenantIdentifier,
        bool isApproved)
    {
        try
        {
            var request = new Phase8Request
            {
                Id = _requestIdCounter++,
                RequestId = requestId,
                UserIdentifier = userIdentifier,
                UseCase = useCase,
                TenantIdentifier = tenantIdentifier,
                IsApproved = isApproved,
                CreatedDate = DateTime.UtcNow
            };

            Requests.Add(request);
            return await Task.FromResult(request.Id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// WBS 8.18 - Store the workflow result.
    /// </summary>
    public async Task SaveResultAsync(
        int requestDbId,
        string phase7RequestId,
        string status,
        string message,
        bool isMock,
        string? additionalData = null)
    {
        try
        {
            var result = new Phase8Result
            {
                Id = _resultIdCounter++,
                RequestId = requestDbId,
                Phase7RequestId = phase7RequestId,
                Status = status,
                Message = message,
                IsMock = isMock,
                AdditionalData = additionalData ?? string.Empty,
                CreatedDate = DateTime.UtcNow
            };

            Results.Add(result);
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// WBS 8.15 - Record an action in audit log.
    /// </summary>
    public async Task LogActionAsync(
        int requestDbId,
        string phase7RequestId,
        string action,
        string actionStatus,
        string details,
        string? errorMessage = null)
    {
        try
        {
            var log = new Phase8AuditLog
            {
                Id = _auditIdCounter++,
                RequestId = requestDbId,
                Phase7RequestId = phase7RequestId,
                Action = action,
                ActionStatus = actionStatus,
                Details = details,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            AuditLogs.Add(log);
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// WBS 8.13 - Retrieve request by Phase 7 request ID (Request and Result Association).
    /// </summary>
    public async Task<Phase8Request?> GetRequestAsync(string phase7RequestId)
    {
        var request = Requests.FirstOrDefault(r => r.RequestId == phase7RequestId);
        return await Task.FromResult(request);
    }

    /// <summary>
    /// WBS 8.14 - Retrieve result by Phase 7 request ID (Result Data Validation).
    /// </summary>
    public async Task<Phase8Result?> GetResultAsync(string phase7RequestId)
    {
        var result = Results.FirstOrDefault(r => r.Phase7RequestId == phase7RequestId);
        return await Task.FromResult(result);
    }

    /// <summary>
    /// WBS 8.20 - Retrieve audit logs for a request (Audit Data Handoff).
    /// </summary>
    public async Task<List<Phase8AuditLog>> GetAuditLogsAsync(string phase7RequestId)
    {
        var logs = AuditLogs
            .Where(l => l.Phase7RequestId == phase7RequestId)
            .OrderBy(l => l.Timestamp)
            .ToList();

        return await Task.FromResult(logs);
    }
}
