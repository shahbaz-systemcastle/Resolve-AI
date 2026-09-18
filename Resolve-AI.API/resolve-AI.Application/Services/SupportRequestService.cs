using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.UseCases.PasswordExpired;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Services;

public class SupportRequestService : ISupportRequestService
{
    private readonly PasswordExpiredWorkflow _passwordExpiredWorkflow;

    public SupportRequestService(
        PasswordExpiredWorkflow passwordExpiredWorkflow)
    {
        _passwordExpiredWorkflow = passwordExpiredWorkflow;
    }

    public async Task<SupportResponseDto> ProcessRequestAsync(
        SupportRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new SupportResponseDto
            {
                Success = false,
                Message = "Support request message is required.",
                UseCase = 0
            };
        }

        var message = request.Message.Trim().ToLowerInvariant();

        // UC-01: Password Expired
        if (message.Contains("password") &&
            (message.Contains("expired") ||
             message.Contains("expire")))
        {
            var result = await _passwordExpiredWorkflow
                .ExecuteAsync("test.user");

            return new SupportResponseDto
            {
                Success = result,
                Message = result
                    ? "Password reset successfully."
                    : "Unable to reset the password.",
                UseCase = UseCaseType.PasswordExpired
            };
        }

        return new SupportResponseDto
        {
            Success = false,
            Message = "Unable to identify the support request.",
            UseCase = 0
        };
    }
}