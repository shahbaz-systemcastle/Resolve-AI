namespace resolve_AI.Application.DTOs;

/// <summary>
/// Machine-readable outcome codes for a Phase 7 workflow result.
/// Success alone is not enough: orchestration needs to know whether
/// a license is verified, unverified, or was never touched.
/// </summary>
public static class Phase7Status
{
    public const string Completed = "COMPLETED";
    public const string NoActionRequired = "NO_ACTION_REQUIRED";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Microsoft365Unavailable = "M365_UNAVAILABLE";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserDisabled = "USER_DISABLED";
    public const string LicenseReviewRequired = "LICENSE_REVIEW_REQUIRED";
    public const string UsageLocationMissing = "USAGE_LOCATION_MISSING";
    public const string NotApproved = "NOT_APPROVED";
    public const string AssignedUnverified = "ASSIGNED_UNVERIFIED";
    public const string MailboxDataNotFound = "MAILBOX_DATA_NOT_FOUND";
    public const string CapacityAnalysed = "CAPACITY_ANALYSED";
    public const string CapacityUnknown = "CAPACITY_UNKNOWN";
    public const string WorkflowError = "WORKFLOW_ERROR";

    // Mailbox action result codes (WBS 7.14 and 7.15), in MailboxActionResultDto.Status.
    public const string MailboxActionLoggedOnly = "LOGGED_ONLY";
    public const string MailboxActionFailed = "FAILED";
}
