using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary structured workflow request for Phase 7.
///
/// MOCK/TBD:
/// This contract is temporary and will be aligned with the final
/// Automation Orchestration request contract when confirmed.
/// </summary>
public class Phase7WorkflowRequestDto
{
    /// <summary>
    /// MOCK/TBD:
    /// Temporary request/correlation identifier.
    /// Final format will be provided by Automation Orchestration.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7 currently supports:
    /// UC-07 Microsoft 365 License Not Assigned
    /// UC-08 Mailbox Capacity Threshold Alert
    /// </summary>
    public UseCaseType UseCase { get; set; }

    /// <summary>
    /// MOCK/TBD:
    /// Temporary identifier for the Microsoft 365 user.
    /// Final identity field and format are not yet confirmed.
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// MOCK/TBD:
    /// Tenant information may be required for Microsoft 365 integration.
    /// Final requirement is not yet confirmed.
    /// </summary>
    public string? TenantIdentifier { get; set; }

    /// <summary>
    /// MOCK/TBD:
    /// Temporary representation of upstream approval.
    /// Final approval contract and rules are not yet confirmed.
    /// </summary>
    public bool? IsApproved { get; set; }
}