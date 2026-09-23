using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Temporary mock implementation for Phase 7 Microsoft 365 integration.
///
/// MOCK/TBD:
/// Replace with real Microsoft Graph implementation when
/// tenant configuration, credentials and permissions are confirmed.
/// </summary>
public class MockMicrosoft365IntegrationService
    : IMicrosoft365IntegrationService
{
    public Task<bool> CheckAvailabilityAsync(
        string? tenantIdentifier)
    {
        // MOCK/TBD:
        // No real Microsoft 365 connection is performed.

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
            DisplayName = "Mock Phase 7 User",
            Found = true,
            IsMock = true
        };

        return Task.FromResult(user);
    }
}