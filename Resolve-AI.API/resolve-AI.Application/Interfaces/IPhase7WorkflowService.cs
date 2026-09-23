using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

public interface IPhase7WorkflowService
{
    Task<Phase7WorkflowResultDto> ProcessAsync(
        Phase7WorkflowRequestDto request);
}