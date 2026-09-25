namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary Microsoft 365 license status model.
///
/// MOCK/TBD:
/// Exact Microsoft 365 SKU and final license fields
/// are not yet confirmed.
/// </summary>
public class Microsoft365LicenseStatusDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    public bool HasAssignedLicense { get; set; }

    public string LicenseStatus { get; set; } = string.Empty;

    public bool IsMock { get; set; } = true;
}