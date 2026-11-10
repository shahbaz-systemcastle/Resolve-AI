namespace resolve_AI.Application.Services.Phase4;

using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;
using resolve_AI.Domain.Enums;

public class Phase4RoutingResolver : IPhase4RoutingResolver
{
    public async Task<AutomationRoute> ResolveRouteAsync(AutomationRequest request)
    {
        var targetPhase = DetermineTargetPhase(request.UseCase);

        return await Task.FromResult(new AutomationRoute
        {
            RequestId = request.Id,
            TargetPhase = targetPhase,
            Reason = $"Routing UC-{(int)request.UseCase:D2} to Phase {targetPhase}",
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        });
    }

    private int DetermineTargetPhase(UseCaseType useCase)
    {
        return useCase switch
        {
            // UC 1-3: Device/AD management (Phase 5)
            UseCaseType.PasswordExpired => 5,
            UseCaseType.ForgottenPassword => 5,
            UseCaseType.IncorrectHostname => 5,

            // UC 4-6: MFA/Entra ID (Phase 6)
            UseCaseType.DuplicateComputerAccountOnADRejoin => 6,
            UseCaseType.DisabledUserAccount => 6,
            UseCaseType.MfaResetAfterMobilePhoneChange => 6,

            // UC 7-8: License/Mailbox (Phase 7)
            UseCaseType.Microsoft365LicenseNotAssigned => 7,
            UseCaseType.MailboxCapacityThresholdAlert => 7,

            // UC 9-11: Mixed (needs multiple phases)
            UseCaseType.MfaNotConfigured => 6, // Phase 6
            UseCaseType.HostnameNotBoundToADUser => 5, // Phase 5
            UseCaseType.AccountLockout => 5, // Phase 5

            _ => throw new ArgumentException($"Unknown use case: {useCase}")
        };
    }
}
