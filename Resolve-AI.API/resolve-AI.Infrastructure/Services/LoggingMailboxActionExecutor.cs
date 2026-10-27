using Microsoft.Extensions.Logging;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// WBS 7.14 - Approved Mailbox Action, default implementation.
///
/// Default parameters, used until the real action is defined (Phase 3 and 4
/// are not complete). It only logs the approved action and changes no mailbox.
/// Replace this class in Program.cs when the action is confirmed.
/// </summary>
public class LoggingMailboxActionExecutor : IMailboxActionExecutor
{
    private readonly ILogger<LoggingMailboxActionExecutor> _logger;

    public LoggingMailboxActionExecutor(
        ILogger<LoggingMailboxActionExecutor> logger)
    {
        _logger = logger;
    }

    public Task<MailboxActionResultDto> ExecuteAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Approved mailbox action for alert {AlertId} ({User}, {Severity}). Logged only; no mailbox was changed.",
            alert.AlertId,
            alert.UserIdentifier,
            alert.Severity);

        return Task.FromResult(new MailboxActionResultDto
        {
            AlertId = alert.AlertId,
            Status = "LOGGED_ONLY",
            Message = "Approved action was logged. No mailbox change is defined yet."
        });
    }
}
