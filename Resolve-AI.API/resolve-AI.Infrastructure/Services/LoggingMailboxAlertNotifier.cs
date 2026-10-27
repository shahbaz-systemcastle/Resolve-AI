using Microsoft.Extensions.Logging;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// WBS 7.13b - Notification (extra step), default implementation.
///
/// It only writes the alert to the log and does not send any message.
/// Real delivery (email or Teams) needs the channel and the recipients
/// to be confirmed first; then a new implementation replaces this one
/// in Program.cs.
/// </summary>
public class LoggingMailboxAlertNotifier : IMailboxAlertNotifier
{
    private readonly ILogger<LoggingMailboxAlertNotifier> _logger;

    public LoggingMailboxAlertNotifier(
        ILogger<LoggingMailboxAlertNotifier> logger)
    {
        _logger = logger;
    }

    public Task<MailboxAlertNotificationResultDto> NotifyAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Mailbox capacity alert {AlertId}: {User} is {Severity} at {Usage}% (threshold {Threshold}%). Not sent: no channel configured.",
            alert.AlertId,
            alert.UserIdentifier,
            alert.Severity,
            alert.UsagePercent,
            alert.ThresholdPercent);

        return Task.FromResult(new MailboxAlertNotificationResultDto
        {
            AlertId = alert.AlertId,
            Status = "NOT_SENT",
            Channel = "LOG",
            Message = "No notification channel is configured; the alert was only logged."
        });
    }
}
