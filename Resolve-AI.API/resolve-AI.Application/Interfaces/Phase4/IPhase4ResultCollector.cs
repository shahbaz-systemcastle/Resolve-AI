namespace resolve_AI.Application.Interfaces.Phase4;

using resolve_AI.Domain.Entities.Phase4;

public interface IPhase4ResultCollector
{
    void AddResult(int phaseNumber, ExecutionResult result);
    ExecutionResult AggregateResults();
}
