using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

public class SupportResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public UseCaseType UseCase { get; set; }
}