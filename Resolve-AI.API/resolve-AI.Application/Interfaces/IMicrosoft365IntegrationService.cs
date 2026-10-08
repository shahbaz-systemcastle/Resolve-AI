using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Defines the Microsoft 365 integration boundary
/// for the License and Mailbox Management Module.
///
/// MOCK/TBD:
/// Final authentication, tenant configuration,
/// Microsoft Graph permissions and service contract
/// are not yet confirmed.
/// </summary>
public interface IMicrosoft365IntegrationService
{
    Task<bool> CheckAvailabilityAsync(
        string? tenantIdentifier);

    Task<List<Microsoft365UserDto>> GetUsersListAsync(
        string? tenantIdentifier);

    Task<Microsoft365UserDto> GetUserAsync(
        string userIdentifier,
        string? tenantIdentifier);

    Task<Microsoft365LicenseStatusDto> GetLicenseStatusAsync(
        string userIdentifier,
        string? tenantIdentifier);

    Task<Microsoft365LicenseAssignmentResultDto> AssignLicenseAsync(
        string userIdentifier,
        string? tenantIdentifier,
        bool isApproved);
}