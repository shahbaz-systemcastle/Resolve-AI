namespace resolve_AI.Application.DTOs;

/// <summary>
/// Result of mailbox capacity analysis (WBS 7.12).
/// State is one of: UNKNOWN, NORMAL, WARNING, CRITICAL.
/// </summary>
public class MailboxCapacityResultDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public string State { get; set; } = "UNKNOWN";

    public double? UsagePercent { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsMock { get; set; } = true;
}
