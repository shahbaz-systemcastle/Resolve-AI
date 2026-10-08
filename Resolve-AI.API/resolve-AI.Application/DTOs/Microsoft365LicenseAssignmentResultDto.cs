namespace resolve_AI.Application.DTOs;

/// <summary>
/// Result of License Assignment (WBS 7.7)
/// for the License and Mailbox Management Module.
///
/// MOCK/TBD:
/// Final assignment outcome contract is not yet confirmed and
/// will be aligned with the Automation Orchestration and
/// Result/Logging contracts when available.
/// </summary>
public class Microsoft365LicenseAssignmentResultDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string AssignmentStatus { get; set; } = string.Empty;

    public string? AssignedSku { get; set; }

    public bool IsMock { get; set; } = true;
}