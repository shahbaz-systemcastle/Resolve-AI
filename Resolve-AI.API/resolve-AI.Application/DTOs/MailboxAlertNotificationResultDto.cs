namespace resolve_AI.Application.DTOs;

/// <summary>
/// Result of sending a mailbox capacity alert (WBS 7.13b).
/// Status is one of: SENT, NOT_SENT (no channel configured), FAILED.
/// </summary>
public class MailboxAlertNotificationResultDto
{
    public string AlertId { get; set; } = string.Empty;

    public string Status { get; set; } = "NOT_SENT";

    public string Channel { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
