namespace resolve_AI.Infrastructure.Services.Phase4;

using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;

public class Phase6ExecutionProxy : IPhase6ExecutionProxy
{
    public async Task<ExecutionResult> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(new ExecutionResult
            {
                RequestId = request.Id,
                PhaseNumber = 6,
                Status = "NotImplemented",
                Data = "Phase 6 (MFA/Entra ID) not yet implemented",
                ExecutedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                RequestId = request.Id,
                PhaseNumber = 6,
                Status = "Failed",
                Error = ex.Message,
                ExecutedAt = DateTime.UtcNow
            };
        }
    }
}
