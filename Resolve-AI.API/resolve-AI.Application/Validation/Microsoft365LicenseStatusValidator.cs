using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.07 - License Status Validation.
/// Requires Microsoft 365 Business Premium with active Teams.
/// </summary>
public static class Microsoft365LicenseStatusValidator
{
    public static LicenseStatusValidationResultDto Validate(
        Microsoft365LicenseStatusDto? status)
    {
        if (status == null ||
            string.IsNullOrWhiteSpace(status.UserIdentifier))
        {
            return new LicenseStatusValidationResultDto
            {
                IsValid = false,
                LicenseActionRequired = false,
                Message = "License status or user identifier is missing.",
                IsMock = status?.IsMock ?? false
            };
        }

        var requiredSku = Guid.Parse(
            "cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46");

        var hasRequiredLicense =
            status.AssignedSkuIds.Contains(requiredSku);

        if (!hasRequiredLicense)
        {
            return new LicenseStatusValidationResultDto
            {
                UserIdentifier = status.UserIdentifier,
                IsValid = true,
                LicenseActionRequired = true,
                Message = "Required Business Premium license is missing. "
                          + "Approved assignment is required.",
                IsMock = status.IsMock
            };
        }

        if (status.IsTeamsEnabled != true)
        {
            return new LicenseStatusValidationResultDto
            {
                UserIdentifier = status.UserIdentifier,
                IsValid = false,
                LicenseActionRequired = false,
                Message = "Business Premium is assigned, but Teams is not "
                          + "confirmed active. Service-plan review is required.",
                IsMock = status.IsMock
            };
        }

        return new LicenseStatusValidationResultDto
        {
            UserIdentifier = status.UserIdentifier,
            IsValid = true,
            LicenseActionRequired = false,
            Message = "Business Premium with active Teams is assigned. "
                      + "No license action is required.",
            IsMock = status.IsMock
        };
    }
}
