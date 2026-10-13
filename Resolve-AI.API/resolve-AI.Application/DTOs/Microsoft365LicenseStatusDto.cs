namespace resolve_AI.Application.DTOs;

/// <summary>
/// Retrieved user license status for WBS 7.06.
/// </summary>
public class Microsoft365LicenseStatusDto
{
    public string UserIdentifier { get; set; } = string.Empty;

    // User ke paas koi bhi license hai ya nahi.
    public bool HasAssignedLicense { get; set; }

    // Graph se retrieve kiye gaye actual SKU IDs.
    public List<Guid> AssignedSkuIds { get; set; } = new();

    // Required Microsoft 365 Business Premium SKU.
    public Guid RequiredSkuId { get; set; } =
        Guid.Parse("cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46");

    public bool HasRequiredLicense { get; set; }

    // Null means Teams status has not been checked.
    public bool? IsTeamsEnabled { get; set; }

    public string LicenseStatus { get; set; } = string.Empty;

    public bool IsMock { get; set; } = true;
}
