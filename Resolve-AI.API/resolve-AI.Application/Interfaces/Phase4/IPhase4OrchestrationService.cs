namespace resolve_AI.Application.Interfaces.Phase4;

using resolve_AI.Domain.Entities.Phase4;

public interface IPhase4OrchestrationService
{
    Task<OrchestrationResult> ProcessAsync(
        AutomationRequest request,
        CancellationToken cancellationToken = default);

    Task<AutomationRoute> DetermineRouteAsync(
        AutomationRequest request);
}

public class OrchestrationResult
{
    public string RequestId { get; set; }
    public string Status { get; set; }
    public List<ExecutionResult> PhaseResults { get; set; } = new();
    public string? FinalError { get; set; }
}
