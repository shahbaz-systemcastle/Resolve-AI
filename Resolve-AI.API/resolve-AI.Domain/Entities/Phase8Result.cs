namespace resolve_AI.Domain.Entities;

/// <summary>
/// Phase 8 - Result record storage (WBS 8.18)
/// Stores the outcome of a Phase 7 workflow execution.
/// </summary>
public class Phase8Result
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
    /// Final status from Phase 7 (COMPLETED, FAILED, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the result used mock data.
    /// </summary>
    public bool IsMock { get; set; }

    /// <summary>
    /// Additional JSON data (quota evaluation, alerts, etc.).
    /// </summary>
    public string AdditionalData { get; set; } = string.Empty;

    /// <summary>
    /// Result creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
