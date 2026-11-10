namespace resolve_AI.Application.Services.Phase4;

using resolve_AI.Application.Interfaces.Phase4;

public class Phase4ErrorHandler : IPhase4ErrorHandler
{
    public async Task<ErrorHandlingResult> HandlePhaseErrorAsync(
        Exception error,
        OrchestrationContext context)
    {
        return await Task.FromResult(new ErrorHandlingResult
        {
            ShouldContinue = false,
            ErrorMessage = $"Phase {context.CurrentPhase} failed: {error.Message}",
            RecoveryAction = DetermineRecoveryAction(error, context)
        });
    }

    private string? DetermineRecoveryAction(Exception error, OrchestrationContext context)
    {
        return error.Message switch
        {
            _ when error.Message.Contains("timeout") => "Retry with longer timeout",
            _ when error.Message.Contains("not found") => "Verify input parameters",
            _ when error.Message.Contains("unauthorized") => "Check credentials",
            _ => "Manual intervention required"
        };
    }
}
