namespace resolve_AI.Application.DTOs;

/// <summary>
/// Mailbox storage usage for one user (WBS 7.11).
/// QuotaBytes of 0 means the quota is unknown.
/// </summary>
public class MailboxUsageDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public bool Found { get; set; }

    public long UsedBytes { get; set; }

    public long QuotaBytes { get; set; }

    public bool IsMock { get; set; } = true;
}
