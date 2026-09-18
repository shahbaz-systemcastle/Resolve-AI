using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

public class MockPasswordService : IPasswordService
{
    public Task<bool> ResetExpiredPasswordAsync(string username)
    {
        // Temporary implementation for testing.
        // This will later be replaced by the actual
        // Active Directory password reset implementation.

        return Task.FromResult(true);
    }
}