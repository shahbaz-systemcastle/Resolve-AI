using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

public interface ISupportRequestService
{
    Task<SupportResponseDto> ProcessRequestAsync(SupportRequestDto request);
}