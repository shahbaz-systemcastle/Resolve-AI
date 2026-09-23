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

            // MOCK/TBD:
            // Replace when the final user identity contract is confirmed.
            UserIdentifier = "test.user@mock.local",

            // MOCK/TBD:
            // Final tenant value/source is not confirmed.
            TenantIdentifier = "MOCK-TENANT",

            // MOCK/TBD:
            // Final approval model and rules are not confirmed.
            IsApproved = true
        };
    }

    public static Phase7WorkflowRequestDto CreateUc08Request()
    {
        return new Phase7WorkflowRequestDto
        {
            RequestId = "MOCK-UC08-001",
            UseCase = UseCaseType.MailboxCapacityThresholdAlert,

            // MOCK/TBD:
            // Replace when the final user identity contract is confirmed.
            UserIdentifier = "test.user@mock.local",

            // MOCK/TBD:
            // Final tenant value/source is not confirmed.
            TenantIdentifier = "MOCK-TENANT",

            // MOCK/TBD:
            // Final approval model and rules are not confirmed.
            IsApproved = true
        };
    }
}