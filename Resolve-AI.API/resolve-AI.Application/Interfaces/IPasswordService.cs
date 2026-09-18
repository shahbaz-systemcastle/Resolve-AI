namespace resolve_AI.Application.Interfaces;

public interface IPasswordService
{
    Task<bool> ResetExpiredPasswordAsync(string username);
}