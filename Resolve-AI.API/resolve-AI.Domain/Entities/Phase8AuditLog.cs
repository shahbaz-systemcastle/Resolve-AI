namespace resolve_AI.Domain.Entities;

/// <summary>
/// Phase 8 - Audit trail (WBS 8.15)
/// Records each action taken during workflow processing.
/// Supports compliance, debugging, and historical inquiry.
/// </summary>
public class Phase8AuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Phase8Request.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// Request ID (for reference without joining).
    /// </summary>
    public string Phase7RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Action name (e.g., "validation", "assignment", "notification").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Action status (success, failed, skipped).
    /// </summary>
    public string ActionStatus { get; set; } = "success";

    /// <summary>
    /// Detailed description of what happened.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Optional error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Log timestamp (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
