using Microsoft.Extensions.Logging;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.Infrastructure.Services;

/// <summary>
/// Phase 7 orchestration handoff (WBS 7.17).
///
/// Validates the complete workflow result before handoff to Phase 4.
/// Ensures all required fields are present and logs the handoff for audit trail.
/// </summary>
public class Phase7HandoffService : IPhase7HandoffService
{
    private readonly ILogger<Phase7HandoffService> _logger;

    public Phase7HandoffService(ILogger<Phase7HandoffService> logger)
    {
        _logger = logger;
    }

    public async Task<Phase7WorkflowResultDto> PrepareHandoffAsync(
        Phase7WorkflowResultDto result)
    {
        // 7.17 - Orchestration Result Handoff

        if (result == null)
        {
            _logger.LogError("Phase 7 handoff received null result.");
            return new Phase7WorkflowResultDto
            {
                Success = false,
                Status = Phase7Status.WorkflowError,
                Message = "Workflow result cannot be null.",
                IsMock = false
            };
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(result.RequestId))
        {
            _logger.LogError("Phase 7 handoff: RequestId is missing.");
            result.Status = Phase7Status.WorkflowError;
            result.Message = "RequestId is required in workflow result.";
            result.Success = false;
            return result;
        }

        if (string.IsNullOrWhiteSpace(result.Status))
        {
            _logger.LogError("Phase 7 handoff: Status is missing.");
            result.Status = Phase7Status.WorkflowError;
            result.Message = "Status is required in workflow result.";
            result.Success = false;
            return result;
        }

        // Log the handoff for audit trail (Phase 8)
        _logger.LogInformation(
            "Phase 7 handoff: RequestId={RequestId}, UseCase={UseCase}, "
            + "Status={Status}, Success={Success}, IsMock={IsMock}",
            result.RequestId,
            result.UseCase,
            result.Status,
            result.Success,
            result.IsMock);

        if (result.LicenseSnapshot != null)
        {
            _logger.LogInformation(
                "Phase 7 handoff license snapshot: Total={TotalSeats}, "
                + "Consumed={ConsumedSeats}, Available={AvailableSeats}",
                result.LicenseSnapshot.TotalSeats,
                result.LicenseSnapshot.ConsumedSeats,
                result.LicenseSnapshot.AvailableSeats);
        }

        if (result.QuotaEvaluation != null)
        {
            _logger.LogInformation(
                "Phase 7 handoff mailbox capacity: UsagePercent={UsagePercent}, "
                + "Decision={Decision}",
                result.QuotaEvaluation.UsagePercent,
                result.QuotaEvaluation.Decision);
        }

        // Result is ready for Phase 4 handoff
        return await Task.FromResult(result);
    }
}
