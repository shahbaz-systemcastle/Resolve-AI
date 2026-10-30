namespace resolve_AI.Domain.Entities;

/// <summary>
/// Phase 8 - Request record storage (WBS 8.17)
/// Stores the original workflow request for audit trail.
/// </summary>
public class Phase8Request
{
    public int Id { get; set; }

    /// <summary>
    /// Unique request identifier from Phase 7.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// User identifier (email or UPN).
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Use case: 7 (License) or 8 (Mailbox).
    /// </summary>
    public int UseCase { get; set; }

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the action was approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Request creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
