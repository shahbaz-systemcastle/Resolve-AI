namespace resolve_AI.Application.DTOs;

/// <summary>
/// Mailbox capacity alert record (WBS 7.13a).
/// Created only when quota evaluation (7.13) requires an alert.
/// Nothing is sent from here; notification is 7.13b.
/// </summary>
public class MailboxCapacityAlertDto
{
    /// <summary>
    /// Same user, severity and day give the same id, so a repeated run
    /// does not look like a new alert.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>WARNING or CRITICAL.</summary>
    public string Severity { get; set; } = string.Empty;

    public double? UsagePercent { get; set; }

    public double ThresholdPercent { get; set; }

    /// <summary>OPEN until notification (7.13b) handles it.</summary>
    public string Status { get; set; } = "OPEN";

    public DateTime CreatedAtUtc { get; set; }
}
