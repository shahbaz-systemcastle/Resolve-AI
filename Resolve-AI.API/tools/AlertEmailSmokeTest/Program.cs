using Microsoft.Extensions.Logging.Abstractions;
using resolve_AI.Application.DTOs;
using resolve_AI.Infrastructure.Services;

var recipient = args.Length > 0
    ? args[0]
    : throw new ArgumentException("Pass the real tenant mailbox recipient.");

var notifier = new GraphMailboxAlertNotifier(
    NullLogger<GraphMailboxAlertNotifier>.Instance);

var result = await notifier.NotifyAsync(new MailboxCapacityAlertDto
{
    AlertId = $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
    UserIdentifier = recipient,
    Severity = "CRITICAL",
    UsagePercent = 95,
    ThresholdPercent = 90,
    Status = "OPEN",
    CreatedAtUtc = DateTime.UtcNow
});

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Channel: {result.Channel}");
Console.WriteLine($"Message: {result.Message}");
