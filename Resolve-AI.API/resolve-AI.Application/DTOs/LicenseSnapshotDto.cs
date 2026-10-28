namespace resolve_AI.Application.DTOs;

/// <summary>
/// Snapshot of license (subscription) status at the moment of workflow execution.
/// Set for UC-07 results to show organization's seat availability.
/// Used for audit trail and Phase 4/8 reporting (WBS 7.17).
/// </summary>
public class LicenseSnapshotDto
{
    /// <summary>
    /// Total Business Premium seats purchased by the organization.
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Number of Business Premium seats currently assigned to users.
    /// </summary>
    public int ConsumedSeats { get; set; }

    /// <summary>
    /// Available seats = TotalSeats - ConsumedSeats.
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Timestamp when this snapshot was captured.
    /// </summary>
    public DateTime SnapshotAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True if this snapshot came from the mock; false if from real Microsoft 365.
    /// </summary>
    public bool IsMock { get; set; } = false;
}
