using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Sends mailbox capacity alerts through Microsoft Graph email.
/// The sender mailbox is configured with ALERT_SENDER_USER. The target user
/// comes from the alert, so no recipient is silently hardcoded.
/// </summary>
public sealed class GraphMailboxAlertNotifier : IMailboxAlertNotifier
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _senderUser;
    private readonly ILogger<GraphMailboxAlertNotifier> _logger;

    public GraphMailboxAlertNotifier(
        ILogger<GraphMailboxAlertNotifier> logger)
    {
        _logger = logger;

        var tenantId = Required("AZURE_TENANT_ID");
        var clientId = Required("AZURE_CLIENT_ID");
        var clientSecret = Required("AZURE_CLIENT_SECRET");
        // Keep the API usable for normal mailboxes when email is not yet
        // configured; an alert will return a clear FAILED result instead of
        // silently pretending that it was sent.
        _senderUser = Environment.GetEnvironmentVariable("ALERT_SENDER_USER")
                      ?? string.Empty;

        _graphClient = new GraphServiceClient(
            new ClientSecretCredential(tenantId, clientId, clientSecret));
    }

    public async Task<MailboxAlertNotificationResultDto> NotifyAsync(
        MailboxCapacityAlertDto alert,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_senderUser))
        {
            return new MailboxAlertNotificationResultDto
            {
                AlertId = alert.AlertId,
                Status = "FAILED",
                Channel = "EMAIL",
                Message = "ALERT_SENDER_USER is not configured."
            };
        }

        var usage = alert.UsagePercent?.ToString("0.0") ?? "unknown";
        var subject = $"Action required: mailbox storage is {usage}% full";
        var body = $"Your Microsoft 365 mailbox is currently {usage}% full.\n\n"
                   + "Please review old messages and archive or delete unnecessary items, "
                   + "or contact IT to request a quota increase.\n\n"
                   + $"Alert ID: {alert.AlertId}\n"
                   + $"Severity: {alert.Severity}\n"
                   + $"Threshold: {alert.ThresholdPercent:0.0}%";

        try
        {
            await _graphClient.Users[_senderUser].SendMail.PostAsync(
                new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Text,
                            Content = body
                        },
                        ToRecipients = new List<Recipient>
                        {
                            new()
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = alert.UserIdentifier
                                }
                            }
                        }
                    },
                    SaveToSentItems = true
                },
                requestConfiguration: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Mailbox alert email sent. AlertId={AlertId}, Recipient={Recipient}, Sender={Sender}.",
                alert.AlertId, alert.UserIdentifier, _senderUser);

            return new MailboxAlertNotificationResultDto
            {
                AlertId = alert.AlertId,
                Status = "SENT",
                Channel = "EMAIL",
                Message = "Mailbox capacity alert email sent to the user."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Mailbox alert email failed. AlertId={AlertId}, Recipient={Recipient}.",
                alert.AlertId, alert.UserIdentifier);

            return new MailboxAlertNotificationResultDto
            {
                AlertId = alert.AlertId,
                Status = "FAILED",
                Channel = "EMAIL",
                Message = $"Mailbox alert email failed: {ex.Message}"
            };
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is not configured.");
        return value;
    }
}
