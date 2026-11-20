namespace resolve_AI.Application.DTOs;

/// <summary>
/// Final result returned to Automation (Enterprise Service contract).
///
/// Status is one of: SucceededVerified, Failed, Rejected, PartiallySucceeded, Unknown.
/// SucceededVerified is only used when the observed state is backed by a
/// VerificationEvidenceReference. Without evidence the result is Unknown.
/// </summary>
public sealed record EnterpriseServiceResultDto(
    int SchemaVersion,
    Guid OperationId,
    string Status,
    string OutcomeCode,
    string? VerificationEvidenceReference,
    DateTimeOffset CompletedAtUtc)
{
    public const string SucceededVerified = "SucceededVerified";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";
    public const string PartiallySucceeded = "PartiallySucceeded";
    public const string Unknown = "Unknown";

    public static EnterpriseServiceResultDto FromPhase7Result(
        Phase7WorkflowResultDto phase7Result,
        Guid operationId)
    {
        var (status, evidence) = Map(phase7Result, operationId);

        return new EnterpriseServiceResultDto(
            SchemaVersion: 1,
            OperationId: operationId,
            Status: status,
            OutcomeCode: phase7Result.Status,
            VerificationEvidenceReference: evidence,
            CompletedAtUtc: DateTimeOffset.UtcNow
        );
    }

    public static EnterpriseServiceResultDto Rejection(Guid operationId, string outcomeCode)
    {
        return new EnterpriseServiceResultDto(
            SchemaVersion: 1,
            OperationId: operationId,
            Status: Rejected,
            OutcomeCode: outcomeCode,
            VerificationEvidenceReference: null,
            CompletedAtUtc: DateTimeOffset.UtcNow
        );
    }

    private static (string Status, string? Evidence) Map(
        Phase7WorkflowResultDto result,
        Guid operationId)
    {
        switch (result.Status)
        {
            case Phase7Status.Completed:
            case Phase7Status.NoActionRequired:
                return Verified(result, operationId);

            case Phase7Status.CapacityAnalysed:
                // An alert that was not sent or acted on is only partly done.
                var alertDone = result.Alert == null
                    || (result.AlertNotification?.Status == "SENT"
                        && result.MailboxAction?.Status == "EXECUTED");

                if (!alertDone)
                {
                    return (PartiallySucceeded, MailboxEvidence(result, operationId));
                }
                return Verified(result, operationId);

            case Phase7Status.AssignedUnverified:
                return (Unknown, null);

            case Phase7Status.NotApproved:
            case Phase7Status.ValidationFailed:
            case Phase7Status.UserNotFound:
            case Phase7Status.UserDisabled:
            case Phase7Status.LicenseReviewRequired:
            case Phase7Status.UsageLocationMissing:
            case Phase7Status.MailboxDataNotFound:
            case Phase7Status.NoFreeSeats:
            case Phase7Status.SkuNotEnabled:
            case Phase7Status.SkuNotAvailable:
            case Phase7Status.TeamsNotAvailable:
            case Phase7Status.TeamsReviewRequired:
                return (Rejected, null);

            default:
                // System errors (M365 unavailable, workflow error, capacity unknown).
                return (Failed, null);
        }
    }

    // A success without evidence is downgraded to Unknown, never reported as verified.
    private static (string Status, string? Evidence) Verified(
        Phase7WorkflowResultDto result,
        Guid operationId)
    {
        var evidence = result.LicenseSnapshot != null
            ? $"m365-license-snapshot/{operationId}/{result.LicenseSnapshot.SnapshotAtUtc:O}"
            : result.QuotaEvaluation != null
                ? MailboxEvidence(result, operationId)
                : null;

        return evidence == null
            ? (Unknown, null)
            : (SucceededVerified, evidence);
    }

    private static string? MailboxEvidence(Phase7WorkflowResultDto result, Guid operationId)
    {
        return result.QuotaEvaluation == null
            ? null
            : $"m365-mailbox-usage/{operationId}/{result.QuotaEvaluation.UsagePercent}";
    }
}
