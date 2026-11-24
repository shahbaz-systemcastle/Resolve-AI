using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Services;
using resolve_AI.Domain.Enums;

public class FakeM365 : IMicrosoft365IntegrationService
{
    public long Used, Quota;
    public Task<bool> CheckAvailabilityAsync(string t) => Task.FromResult(true);
    public Task<List<Microsoft365UserDto>> GetUsersListAsync(string t) => Task.FromResult(new List<Microsoft365UserDto>());
    public Task<Microsoft365UserDto> GetUserAsync(string u, string t) => Task.FromResult(new Microsoft365UserDto { UserIdentifier = u, DisplayName = "Fake", Found = true, AccountEnabled = true, UsageLocation = "US", IsMock = true });
    public Task<Microsoft365LicenseStatusDto> GetLicenseStatusAsync(string u, string t) => Task.FromResult(new Microsoft365LicenseStatusDto { UserIdentifier = u, IsMock = true });
    public Task<Microsoft365LicenseAssignmentResultDto> AssignLicenseAsync(string u, string t, bool a) => throw new InvalidOperationException("not used");
    public Task<MailboxUsageDto> GetMailboxUsageAsync(string u, string t) => Task.FromResult(new MailboxUsageDto { UserIdentifier = u, Found = true, UsedBytes = Used, QuotaBytes = Quota, IsMock = true });
    public Task<LicenseSnapshotDto> GetLicenseSubscriptionSnapshotAsync(string t) => Task.FromResult(new LicenseSnapshotDto { TotalSeats = 100, ConsumedSeats = 50, AvailableSeats = 50, SnapshotAtUtc = DateTime.UtcNow, IsMock = true });
}

public class FakeHandoff : IPhase7HandoffService
{
    public Task<Phase7WorkflowResultDto> PrepareHandoffAsync(Phase7WorkflowResultDto result) => Task.FromResult(result);
}

public class FakeStorage : IPhase8StorageService
{
    public Task<int> SaveRequestAsync(string requestId, string userIdentifier, int useCase, string tenantIdentifier, bool isApproved) => Task.FromResult(1);
    public Task SaveResultAsync(int requestDbId, string phase7RequestId, string status, string message, bool isMock, string additionalData = null) => Task.CompletedTask;
    public Task LogActionAsync(int requestDbId, string phase7RequestId, string action, string actionStatus, string details, string errorMessage = null) => Task.CompletedTask;
    public Task<resolve_AI.Domain.Entities.Phase8Request> GetRequestAsync(string phase7RequestId) => Task.FromResult<resolve_AI.Domain.Entities.Phase8Request>(null);
    public Task<resolve_AI.Domain.Entities.Phase8Result> GetResultAsync(string phase7RequestId) => Task.FromResult<resolve_AI.Domain.Entities.Phase8Result>(null);
    public Task<List<resolve_AI.Domain.Entities.Phase8AuditLog>> GetAuditLogsAsync(string phase7RequestId) => Task.FromResult(new List<resolve_AI.Domain.Entities.Phase8AuditLog>());
}

public class RecordingNotifier : IMailboxAlertNotifier
{
    public int Calls;
    public Task<MailboxAlertNotificationResultDto> NotifyAsync(MailboxCapacityAlertDto alert, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult(new MailboxAlertNotificationResultDto { AlertId = alert.AlertId, Status = "NOT_SENT", Channel = "LOG", Message = "logged" });
    }
}

public class FakeExecutor : IMailboxActionExecutor
{
    public Func<MailboxCapacityAlertDto, MailboxActionResultDto> Produce;
    public int Calls;
    public Task<MailboxActionResultDto> ExecuteAsync(MailboxCapacityAlertDto alert, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult(Produce(alert));
    }
}

public static class Harness
{
    public static async Task Run(string name, long used, long quota, bool approved, Func<MailboxCapacityAlertDto, MailboxActionResultDto> produce)
    {
        var notifier = new RecordingNotifier();
        var executor = new FakeExecutor { Produce = produce };
        var svc = new Phase7WorkflowService(
            new FakeM365 { Used = used, Quota = quota },
            notifier,
            executor,
            new FakeHandoff(),
            new FakeStorage());
        var r = await svc.ProcessAsync(new Phase7WorkflowRequestDto { RequestId = "H-" + name, UseCase = UseCaseType.MailboxCapacityThresholdAlert, UserIdentifier = "user@x.com", IsApproved = approved });
        Console.WriteLine($"[{name}] workflow={r.Status} success={r.Success}");
        Console.WriteLine($"   7.13  decision   = {r.QuotaEvaluation?.Decision} ({r.QuotaEvaluation?.CapacityState})");
        Console.WriteLine($"   7.13a alert      = {(r.Alert == null ? "none" : r.Alert.Severity + " threshold " + r.Alert.ThresholdPercent + " " + r.Alert.Status)}");
        Console.WriteLine($"   7.13b notify     = {(r.AlertNotification == null ? "not called" : r.AlertNotification.Status + " via " + r.AlertNotification.Channel)} (calls {notifier.Calls})");
        Console.WriteLine($"   7.14  action     = {r.MailboxAction?.Status ?? "none"} (executor calls {executor.Calls})");
        Console.WriteLine($"   7.15  check      = {r.MailboxAction?.Message ?? "-"}");
    }
}

public static class Entry
{
    static MailboxActionResultDto Ok(MailboxCapacityAlertDto a) => new MailboxActionResultDto { AlertId = a.AlertId, Status = "LOGGED_ONLY", Message = "logged" };

    public static async Task Main()
    {
        await Harness.Run("NORMAL_40_no_alert", 40, 100, true, Ok);
        await Harness.Run("WARNING_84_approved", 84, 100, true, Ok);
        await Harness.Run("WARNING_84_not_approved", 84, 100, false, a => throw new InvalidOperationException("must not run"));
        await Harness.Run("CRITICAL_95_approved", 95, 100, true, Ok);
        await Harness.Run("WARNING_84_wrong_alert_id", 84, 100, true, a => new MailboxActionResultDto { AlertId = "OTHER", Status = "LOGGED_ONLY", Message = "x" });
        await Harness.Run("WARNING_84_unknown_status", 84, 100, true, a => new MailboxActionResultDto { AlertId = a.AlertId, Status = "WEIRD", Message = "x" });
        await Harness.Run("UNKNOWN_no_quota", 10, 0, true, Ok);
    }
}
