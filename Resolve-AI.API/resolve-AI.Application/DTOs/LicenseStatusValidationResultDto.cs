namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary result of License Status Validation (WBS 7.6)
/// for the License and Mailbox Management Module.
///
/// MOCK/TBD:
/// Final validation outcome contract is not yet confirmed and
/// will be aligned with the Automation Orchestration and
/// Result/Logging contracts when available.
/// </summary>
public class LicenseStatusValidationResultDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the retrieved license status was
    /// structurally usable. False means the status could not be
    /// interpreted and no decision was made.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// MOCK/TBD:
    /// Indicates whether a license action may be required.
    /// This is currently derived only from HasAssignedLicense.
    /// The real rule (which SKU counts as an assigned license,
    /// entitlement rules, and approval requirements) is not confirmed.
    /// </summary>
    public bool LicenseActionRequired { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsMock { get; set; } = true;
}
