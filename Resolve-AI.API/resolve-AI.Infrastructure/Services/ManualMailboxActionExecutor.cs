using Microsoft.Extensions.Logging;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Creates a real, explicit manual action instruction. It never deletes or
/// archives user mail automatically because those actions require user review.
/// </summary>
public sealed class ManualMailboxActionExecutor : IMailboxActionExecutor
{
    private readonly ILogger<ManualMailboxActionExecutor> _logger;

    public ManualMailboxActionExecutor(
        ILogger<ManualMailboxActionExecutor> logger)
    {
        _logger = logger;
    }

    public Task<MailboxActionResultDto> ExecuteAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default)
    {
        const string message =
            "Manual action required: review the mailbox, archive old mail to PST, "
            + "delete unnecessary items, or request a quota increase. No mailbox "
            + "content was changed automatically.";

        _logger.LogInformation(
            "Manual mailbox action created. AlertId={AlertId}, User={User}, Severity={Severity}.",
            alert.AlertId, alert.UserIdentifier, alert.Severity);

        return Task.FromResult(new MailboxActionResultDto
        {
            AlertId = alert.AlertId,
            Status = "PENDING_USER_ACTION",
            Message = message
        });
    }
}
