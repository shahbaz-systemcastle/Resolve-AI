using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Sends a mailbox capacity alert to its recipients (WBS 7.13b).
/// The implementation decides the channel (for example email or Teams).
/// </summary>
public interface IMailboxAlertNotifier
{
    Task<MailboxAlertNotificationResultDto> NotifyAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default);
}
