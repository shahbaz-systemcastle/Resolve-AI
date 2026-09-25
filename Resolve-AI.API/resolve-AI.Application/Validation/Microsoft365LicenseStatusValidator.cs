using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Validation;

/// <summary>
/// WBS 7.6 - License Status Validation.
///
/// Validates the Microsoft 365 license status that was retrieved
/// in WBS 7.5 and produces a decision for the
/// License and Mailbox Management Module.
///
/// This class contains application logic only.
/// It performs no Microsoft 365 or Microsoft Graph calls, and it
/// executes no license action. Executing an action is WBS 7.7.
///
/// MOCK/TBD:
/// The following rules are NOT implemented here because they are
/// not confirmed:
/// - Exact Microsoft 365 SKU rules
/// - License entitlement rules
/// - Approval rules
/// - Tenant specific licensing rules
/// - Available license quota checks
/// </summary>
public static class Microsoft365LicenseStatusValidator
{
    public static LicenseStatusValidationResultDto Validate(
        Microsoft365LicenseStatusDto? licenseStatus)
    {
        // Structural check: the retrieved status must exist.
        if (licenseStatus is null)
        {
            return new LicenseStatusValidationResultDto
            {
                IsValid = false,
                LicenseActionRequired = false,
                Message = "License status could not be validated "
                          + "because no license status was retrieved.",
                IsMock = true
            };
        }

        // Structural check: the status must identify a user.
        if (string.IsNullOrWhiteSpace(licenseStatus.UserIdentifier))
        {
            return new LicenseStatusValidationResultDto
            {
                UserIdentifier = licenseStatus.UserIdentifier,
                IsValid = false,
                LicenseActionRequired = false,
                Message = "License status could not be validated "
                          + "because UserIdentifier is missing.",
                IsMock = true
            };
        }

        // MOCK/TBD:
        // The decision below is derived only from the
        // HasAssignedLicense flag returned by WBS 7.5.
        // No SKU, entitlement, quota or approval rule is applied.
        if (!licenseStatus.HasAssignedLicense)
        {
            return new LicenseStatusValidationResultDto
            {
                UserIdentifier = licenseStatus.UserIdentifier,
                IsValid = true,
                LicenseActionRequired = true,
                Message = "No Microsoft 365 license is assigned. "
                          + "A license action may be required. "
                          + "MOCK/TBD: exact license rule is not confirmed.",
                IsMock = licenseStatus.IsMock
            };
        }

        return new LicenseStatusValidationResultDto
        {
            UserIdentifier = licenseStatus.UserIdentifier,
            IsValid = true,
            LicenseActionRequired = false,
            Message = "A Microsoft 365 license is already assigned. "
                      + "No license action is required. "
                      + "MOCK/TBD: exact license rule is not confirmed.",
            IsMock = licenseStatus.IsMock
        };
    }
}
