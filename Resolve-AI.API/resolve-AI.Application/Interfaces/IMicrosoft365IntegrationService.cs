using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Defines the Phase 7 Microsoft 365 integration boundary.
///
/// MOCK/TBD:
/// Final authentication, tenant configuration,
/// Graph permissions and service contract are not yet confirmed.
/// </summary>
public interface IMicrosoft365IntegrationService
{
    Task<bool> CheckAvailabilityAsync(
        string? tenantIdentifier);

    Task<Microsoft365UserDto> GetUserAsync(
        string userIdentifier,
        string? tenantIdentifier);
}