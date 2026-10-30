using resolve_AI.Domain.Entities;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Phase 8 - Result and Logging Module (WBS 8.12 - 8.22)
///
/// Handles storage of requests, results, and audit logs.
/// Provides persistence for Phase 7 workflow outcomes.
/// </summary>
public interface IPhase8StorageService
{
    /// <summary>
    /// Store the incoming request (WBS 8.17 - Request Record Storage).
    /// </summary>
    Task<int> SaveRequestAsync(
        string requestId,
        string userIdentifier,
        int useCase,
        string tenantIdentifier,
        bool isApproved);

    /// <summary>
    /// Store the workflow result (WBS 8.18 - Action and Outcome Storage).
    /// </summary>
    Task SaveResultAsync(
        int requestDbId,
        string phase7RequestId,
        string status,
        string message,
        bool isMock,
        string? additionalData = null);

    /// <summary>
    /// Record an action in the audit log (WBS 8.15 - Audit Record Preparation).
    /// </summary>
    Task LogActionAsync(
        int requestDbId,
        string phase7RequestId,
        string action,
        string actionStatus,
        string details,
        string? errorMessage = null);

    /// <summary>
    /// Retrieve request by Phase 7 request ID (for association).
    /// </summary>
    Task<Phase8Request?> GetRequestAsync(string phase7RequestId);

    /// <summary>
    /// Retrieve result by Phase 7 request ID.
    /// </summary>
    Task<Phase8Result?> GetResultAsync(string phase7RequestId);

    /// <summary>
    /// Retrieve audit logs for a request.
    /// </summary>
    Task<List<Phase8AuditLog>> GetAuditLogsAsync(string phase7RequestId);
}
