using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary technical result for Phase 7.
/// Final orchestration/result contract is MOCK/TBD.
/// </summary>
public class Phase7WorkflowResultDto
{
    public string RequestId { get; set; } = string.Empty;

    public UseCaseType UseCase { get; set; }

    public bool Success { get; set; }

    /// <summary>
    /// Outcome code, see <see cref="Phase7Status"/>.
    /// </summary>
    public string Status { get; set; } = Phase7Status.WorkflowError;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Set only for UC-08 after quota evaluation (WBS 7.13).
    /// </summary>
    public QuotaEvaluationResultDto? QuotaEvaluation { get; set; }

    /// <summary>
    /// Set only when quota evaluation requires an alert (WBS 7.13a).
    /// </summary>
    public MailboxCapacityAlertDto? Alert { get; set; }

    /// <summary>
    /// Set only when an alert was created (WBS 7.13b).
    /// </summary>
    public MailboxAlertNotificationResultDto? AlertNotification { get; set; }

    /// <summary>
    /// Set only when an alert exists (WBS 7.14, provisional).
    /// </summary>
    public MailboxActionResultDto? MailboxAction { get; set; }

    /// <summary>
    /// License seat snapshot at workflow execution time (WBS 7.17).
    /// Set for UC-07 results to show organization's seat availability.
    /// Used for audit trail and Phase 4/8 reporting.
    /// </summary>
    public LicenseSnapshotDto? LicenseSnapshot { get; set; }

    /// <summary>
    /// True when any Microsoft 365 answer used in this result came from the mock.
    /// </summary>
    public bool IsMock { get; set; } = false;
}
