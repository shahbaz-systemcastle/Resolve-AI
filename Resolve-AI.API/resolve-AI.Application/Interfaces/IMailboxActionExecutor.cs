using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Runs the approved mailbox action for a capacity alert (WBS 7.14).
/// The concrete action is not confirmed yet; the implementation decides it.
/// </summary>
public interface IMailboxActionExecutor
{
    Task<MailboxActionResultDto> ExecuteAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default);
}
