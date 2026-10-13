using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.10 - Mailbox Capacity Analysis.
///
/// Pure logic: compares used mailbox storage with its quota.
/// It performs no Microsoft 365 or Exchange calls.
///
/// MOCK/TBD:
/// The warning and critical percentages are a business decision
/// that is not confirmed yet.
/// </summary>
public static class MailboxCapacityAnalyzer
{
    public const double WarningPercent = 80;

    public const double CriticalPercent = 90;

    public static MailboxCapacityResultDto Analyze(MailboxUsageDto usage)
    {
        if (!usage.Found || usage.QuotaBytes <= 0)
        {
            return new MailboxCapacityResultDto
            {
                UserIdentifier = usage.UserIdentifier,
                State = "UNKNOWN",
                Message = "Mailbox quota is not available, "
                          + "so capacity cannot be analysed.",
                IsMock = usage.IsMock
            };
        }

        var percent = usage.UsedBytes * 100.0 / usage.QuotaBytes;

        var state = percent >= CriticalPercent ? "CRITICAL"
                  : percent >= WarningPercent ? "WARNING"
                  : "NORMAL";

        return new MailboxCapacityResultDto
        {
            UserIdentifier = usage.UserIdentifier,
            State = state,
            UsagePercent = Math.Round(percent, 1),
            Message = $"Mailbox usage is {percent:0.0}% of quota ({state}).",
            IsMock = usage.IsMock
        };
    }
}
