using resolve_AI.Application.Interfaces;

namespace resolve_AI.Application.UseCases.PasswordExpired;

public class PasswordExpiredWorkflow
{
    private readonly IPasswordService _passwordService;

    public PasswordExpiredWorkflow(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public async Task<bool> ExecuteAsync(string username)
    {
        return await _passwordService
            .ResetExpiredPasswordAsync(username);
    }
}