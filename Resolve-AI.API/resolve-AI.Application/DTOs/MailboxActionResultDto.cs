namespace resolve_AI.Application.DTOs;

/// <summary>
/// Result of the approved mailbox action (WBS 7.14, provisional).
/// Status is one of: NOT_APPROVED (request not approved, nothing run),
/// LOGGED_ONLY (default executor, no mailbox was changed), or a status
/// returned by a real executor later.
/// </summary>
public class MailboxActionResultDto
{
    public string AlertId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
