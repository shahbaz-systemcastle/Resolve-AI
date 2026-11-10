namespace resolve_AI.Application.Interfaces.Phase4;

using resolve_AI.Domain.Entities.Phase4;

public interface IPhase6ExecutionProxy
{
    Task<ExecutionResult> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken = default);
}
