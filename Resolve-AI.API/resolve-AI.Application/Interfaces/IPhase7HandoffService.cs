using resolve_AI.Application.DTOs;

namespace resolve_AI.Application.Interfaces;

/// <summary>
/// Phase 7 orchestration handoff service (WBS 7.17).
///
/// Validates the complete workflow result and prepares it for handoff to Phase 4
/// (Automation Orchestration Module). Ensures all required fields are present
/// and result is ready for downstream processing, routing, and audit logging.
/// </summary>
public interface IPhase7HandoffService
{
    /// <summary>
    /// Validate and prepare the workflow result for Phase 4 handoff.
    ///
    /// Returns the same result if validation passes, or a failed result
    /// with WORKFLOW_ERROR status if validation fails.
    /// </summary>
    Task<Phase7WorkflowResultDto> PrepareHandoffAsync(
        Phase7WorkflowResultDto result);
}
