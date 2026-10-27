using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.15 - Mailbox Action Result Reception.
///
/// Pure logic: checks the result returned by the executor (7.14). A result
/// is accepted only when it belongs to the same alert and has a known status.
/// Anything else becomes FAILED, so a bad result is never reported as success.
/// </summary>
public static class MailboxActionResultValidator
{
    private static readonly HashSet<string> KnownStatuses = new()
    {
        Phase7Status.NotApproved,
        Phase7Status.MailboxActionLoggedOnly,
        "EXECUTED",
        Phase7Status.MailboxActionFailed
    };

    public static MailboxActionResultDto Receive(
        MailboxCapacityAlertDto alert,
        MailboxActionResultDto result)
    {
        if (!string.Equals(result.AlertId, alert.AlertId, StringComparison.Ordinal))
        {
            return Failed(alert, "Action result belongs to a different alert.");
        }

        if (!KnownStatuses.Contains(result.Status))
        {
            return Failed(alert, $"Action result status '{result.Status}' is not known.");
        }

        return result;
    }

    private static MailboxActionResultDto Failed(
        MailboxCapacityAlertDto alert,
        string message)
    {
        return new MailboxActionResultDto
        {
            AlertId = alert.AlertId,
            Status = Phase7Status.MailboxActionFailed,
            Message = message
        };
    }
}
