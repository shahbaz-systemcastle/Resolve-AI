using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary mock workflow requests used to develop Phase 7
/// before the real Automation Orchestration contract is available.
/// </summary>
public static class Phase7WorkflowRequestMock
{
    public static Phase7WorkflowRequestDto CreateUc07Request()
    {
        return new Phase7WorkflowRequestDto
        {
            RequestId = "MOCK-UC07-001",
            UseCase = UseCaseType.Microsoft365LicenseNotAssigned,

            // Known tenant user. Already licensed, so the call makes no change.
            UserIdentifier = "HassanKhan@SystemCastle1.onmicrosoft.com",

            // MOCK/TBD:
            // Final tenant value/source is not confirmed.
            TenantIdentifier = "MOCK-TENANT",

            // Not approved, so this endpoint can never assign a license.
            IsApproved = false
        };
    }

    public static Phase7WorkflowRequestDto CreateUc08Request()
    {
        return new Phase7WorkflowRequestDto
        {
            RequestId = "MOCK-UC08-001",
            UseCase = UseCaseType.MailboxCapacityThresholdAlert,

            // Known tenant user with a mailbox.
            UserIdentifier = "HassanKhan@SystemCastle1.onmicrosoft.com",

            // MOCK/TBD:
            // Final tenant value/source is not confirmed.
            TenantIdentifier = "MOCK-TENANT",

            // Not approved, so no approved mailbox action can run from this endpoint.
            IsApproved = false
        };
    }
}