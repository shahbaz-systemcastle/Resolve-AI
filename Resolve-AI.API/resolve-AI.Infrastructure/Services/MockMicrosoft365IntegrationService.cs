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

    public Task<List<Microsoft365UserDto>> GetUsersListAsync(
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real Microsoft Graph user list retrieval is performed.

        var mockUsers = new List<Microsoft365UserDto>
        {
            new() {
                UserIdentifier = "user1@example.com",
                DisplayName = "Mock User One",
                Found = true,
                IsMock = true
            },
            new() {
                UserIdentifier = "user2@example.com",
                DisplayName = "Mock User Two",
                Found = true,
                IsMock = true
            }
        };

        return Task.FromResult(mockUsers);
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
            AccountEnabled = true,
            UsageLocation = "US",
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

    public Task<Microsoft365LicenseAssignmentResultDto> AssignLicenseAsync(
        string userIdentifier,
        string? tenantIdentifier,
        bool isApproved)
    {
        // MOCK/TBD:
        // No real Microsoft Graph license assignment is performed.
        // Exact Microsoft 365 SKU assignment and approval workflow
        // are not confirmed yet.

        var assignmentResult = new Microsoft365LicenseAssignmentResultDto
        {
            UserIdentifier = userIdentifier,
            Success = true,
            Message = "License assigned successfully (MOCK/TBD)",
            AssignmentStatus = "SUCCESS",
            AssignedSku = "ENTERPRISEPREMIUM",
            IsMock = true
        };

        return Task.FromResult(assignmentResult);
    }

    public Task<MailboxUsageDto> GetMailboxUsageAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real mailbox usage report is read.
        // Values are chosen to fall in the WARNING range (84%).

        var usage = new MailboxUsageDto
        {
            UserIdentifier = userIdentifier,
            Found = true,
            UsedBytes = 42L * 1024 * 1024 * 1024,
            QuotaBytes = 50L * 1024 * 1024 * 1024,
            IsMock = true
        };

        return Task.FromResult(usage);
    }
}