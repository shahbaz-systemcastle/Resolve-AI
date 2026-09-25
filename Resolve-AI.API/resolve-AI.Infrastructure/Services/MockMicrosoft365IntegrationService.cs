using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Temporary mock implementation for Microsoft 365 integration
/// used by the License and Mailbox Management Module.
///
/// MOCK/TBD:
/// Replace with the real Microsoft Graph implementation when
/// tenant configuration, credentials, permissions, and final
/// Microsoft 365 requirements are confirmed.
/// </summary>
public class MockMicrosoft365IntegrationService
    : IMicrosoft365IntegrationService
{
    public Task<bool> CheckAvailabilityAsync(
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real Microsoft 365 or Microsoft Graph connection
        // is performed at this stage.

        return Task.FromResult(true);
    }

    public Task<Microsoft365UserDto> GetUserAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real Microsoft Graph user lookup is performed.

        var user = new Microsoft365UserDto
        {
            UserIdentifier = userIdentifier,
            DisplayName = "Mock License and Mailbox User",
            Found = true,
            IsMock = true
        };

        return Task.FromResult(user);
    }

    public Task<Microsoft365LicenseStatusDto> GetLicenseStatusAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real Microsoft Graph license lookup is performed.
        // Exact Microsoft 365 SKU and license rules are not
        // confirmed yet.

        var licenseStatus = new Microsoft365LicenseStatusDto
        {
            UserIdentifier = userIdentifier,

            // MOCK/TBD:
            // Temporary value used only to test
            // License Status Retrieval.
            HasAssignedLicense = false,

            LicenseStatus = "No license assigned - MOCK/TBD",

            IsMock = true
        };

        return Task.FromResult(licenseStatus);
    }
}