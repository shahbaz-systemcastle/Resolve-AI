using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.13a - Alert Setup (extra step, not in WBS).
///
/// Pure logic: turns an ALERT_REQUIRED decision from 7.13 into an
/// alert record. It performs no Microsoft 365 or Exchange calls and
/// sends nothing.
/// </summary>
public static class MailboxAlertSetup
{
    public static MailboxCapacityAlertDto? Create(
        QuotaEvaluationResultDto evaluation,
        DateTime createdAtUtc)
    {
        if (evaluation.Decision != QuotaEvaluator.AlertRequired)
            return null;

        var threshold = evaluation.CapacityState == "CRITICAL"
            ? MailboxCapacityAnalyzer.CriticalPercent
            : MailboxCapacityAnalyzer.WarningPercent;

        return new MailboxCapacityAlertDto
        {
            AlertId = $"MBX-{evaluation.UserIdentifier}-{evaluation.CapacityState}-"
                      + createdAtUtc.ToString("yyyyMMdd"),
            UserIdentifier = evaluation.UserIdentifier,
            Severity = evaluation.CapacityState,
            UsagePercent = evaluation.UsagePercent,
            ThresholdPercent = threshold,
            Status = "OPEN",
            CreatedAtUtc = createdAtUtc
        };
    }
}
