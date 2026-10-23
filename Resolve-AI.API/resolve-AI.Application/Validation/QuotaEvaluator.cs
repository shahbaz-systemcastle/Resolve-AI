using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.11 - Quota Evaluation.
///
/// Pure logic: decides whether the capacity state from 7.10 requires
/// an alert. WARNING and CRITICAL require one; NORMAL does not.
/// If the capacity could not be analysed, nothing is decided.
///
/// It performs no Microsoft 365 or Exchange calls and sends nothing.
/// </summary>
public static class QuotaEvaluator
{
    public const string AlertRequired = "ALERT_REQUIRED";
    public const string NoAlert = "NO_ALERT";
    public const string NotEvaluated = "NOT_EVALUATED";

    public static QuotaEvaluationResultDto Evaluate(
        MailboxCapacityResultDto capacity)
    {
        var (decision, message) = capacity.State switch
        {
            "CRITICAL" or "WARNING" => (
                AlertRequired,
                $"Mailbox is {capacity.State}; an alert is required."),
            "NORMAL" => (
                NoAlert,
                "Mailbox is NORMAL; no alert is required."),
            _ => (
                NotEvaluated,
                "Capacity state is unknown, so no quota decision was made.")
        };

        return new QuotaEvaluationResultDto
        {
            UserIdentifier = capacity.UserIdentifier,
            Decision = decision,
            CapacityState = capacity.State,
            UsagePercent = capacity.UsagePercent,
            Message = message,
            IsMock = capacity.IsMock
        };
    }
}
