namespace resolve_AI.Infrastructure.Services.Phase4;

using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Application.Validation;
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
            // Request contract check first; nothing reaches Microsoft 365 if it fails.
            var enterpriseRequest = EnterpriseServiceRequestDto.FromAutomationRequest(request);
            var (code, _) = EnterpriseServiceRequestValidator.Validate(enterpriseRequest);

            if (code != null)
            {
                return ToExecutionResult(
                    request,
                    EnterpriseServiceResultDto.Rejection(enterpriseRequest.OperationId, code));
            }

            // Approval comes from the request (default false), never hardcoded.
            var phase7Request = new Phase7WorkflowRequestDto
            {
                RequestId = request.Id,
                UserIdentifier = request.UserId,
                UseCase = request.UseCase,
                TenantIdentifier = "default",
                IsApproved = request.IsApproved
            };

            var phase7Result = await _phase7Service.ProcessAsync(phase7Request);

            var enterpriseResult = EnterpriseServiceResultDto.FromPhase7Result(
                phase7Result,
                enterpriseRequest.OperationId);

            return ToExecutionResult(request, enterpriseResult);
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

    private static ExecutionResult ToExecutionResult(
        AutomationRequest request,
        EnterpriseServiceResultDto result)
    {
        return new ExecutionResult
        {
            RequestId = request.Id,
            PhaseNumber = 7,
            Status = result.Status,
            Data = System.Text.Json.JsonSerializer.Serialize(new
            {
                result.Status,
                result.OutcomeCode,
                result.VerificationEvidenceReference
            }),
            Error = result.Status == EnterpriseServiceResultDto.SucceededVerified
                ? null
                : result.OutcomeCode,
            ExecutedAt = DateTime.UtcNow
        };
    }
}
