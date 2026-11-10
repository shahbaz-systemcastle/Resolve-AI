namespace resolve_AI.Infrastructure.Services.Phase4;

using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;

public class Phase7ExecutionProxy : IPhase7ExecutionProxy
{
    private readonly IPhase7WorkflowService _phase7Service;

    public Phase7ExecutionProxy(IPhase7WorkflowService phase7Service)
    {
        _phase7Service = phase7Service;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        AutomationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var phase7Request = new Phase7WorkflowRequestDto
            {
                RequestId = request.Id,
                UserIdentifier = request.UserId,
                UseCase = request.UseCase,
                TenantIdentifier = "default",
                IsApproved = true
            };

            var phase7Result = await _phase7Service.ProcessAsync(phase7Request);

            var operationId = Guid.Parse(request.Id);
            var enterpriseResult = EnterpriseServiceResultDto.FromPhase7Result(phase7Result, operationId);

            return new ExecutionResult
            {
                RequestId = request.Id,
                PhaseNumber = 7,
                Status = enterpriseResult.Status,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    enterpriseResult.Status,
                    enterpriseResult.OutcomeCode,
                    enterpriseResult.VerificationEvidenceReference
                }),
                Error = enterpriseResult.Status == "SucceededVerified" ? null : enterpriseResult.OutcomeCode,
                ExecutedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                RequestId = request.Id,
                PhaseNumber = 7,
                Status = "Failed",
                Error = ex.Message,
                ExecutedAt = DateTime.UtcNow
            };
        }
    }
}
