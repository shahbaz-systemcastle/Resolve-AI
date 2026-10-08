using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.DTOs;

/// <summary>
/// Temporary technical result for Phase 7.
/// Final orchestration/result contract is MOCK/TBD.
/// </summary>
public class Phase7WorkflowResultDto
{
    public string RequestId { get; set; } = string.Empty;

    public UseCaseType UseCase { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// True when any Microsoft 365 answer used in this result came from the mock.
    /// </summary>
    public bool IsMock { get; set; } = false;
}
