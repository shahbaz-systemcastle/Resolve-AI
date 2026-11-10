namespace resolve_AI.Application.Services.Phase4;

using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;

public class Phase4ResultCollector : IPhase4ResultCollector
{
    private readonly List<ExecutionResult> _results = new();

    public void AddResult(int phaseNumber, ExecutionResult result)
    {
        if (result.PhaseNumber == 0)
            result = new ExecutionResult
            {
                Id = result.Id,
                RequestId = result.RequestId,
                PhaseNumber = phaseNumber,
                Status = result.Status,
                Data = result.Data,
                Error = result.Error,
                ExecutedAt = result.ExecutedAt
            };

        _results.Add(result);
    }

    public ExecutionResult AggregateResults()
    {
        if (!_results.Any())
            return new ExecutionResult
            {
                Status = "NoResults",
                ExecutedAt = DateTime.UtcNow
            };

        var hasErrors = _results.Any(r => r.Status == "Failed");
        var aggregatedData = string.Join(" | ", _results.Select(r => $"Phase {r.PhaseNumber}: {r.Status}"));

        return new ExecutionResult
        {
            RequestId = _results.First().RequestId,
            PhaseNumber = 4,
            Status = hasErrors ? "CompletedWithErrors" : "Success",
            Data = aggregatedData,
            ExecutedAt = DateTime.UtcNow
        };
    }
}
