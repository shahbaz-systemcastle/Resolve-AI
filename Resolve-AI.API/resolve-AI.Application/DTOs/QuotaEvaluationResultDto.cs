namespace resolve_AI.Application.DTOs;

/// <summary>
/// Result of quota evaluation (WBS 7.13).
/// Decision is one of: ALERT_REQUIRED, NO_ALERT, NOT_EVALUATED.
/// The decision only says whether an alert is needed; it does not
/// set up an alert (7.13a) or notify anyone (7.13b).
/// </summary>
public class QuotaEvaluationResultDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public string Decision { get; set; } = "NOT_EVALUATED";

    public string CapacityState { get; set; } = "UNKNOWN";

    public double? UsagePercent { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsMock { get; set; } = true;
}
