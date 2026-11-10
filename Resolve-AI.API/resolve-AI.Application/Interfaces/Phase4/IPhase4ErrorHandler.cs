namespace resolve_AI.Application.Interfaces.Phase4;

public interface IPhase4ErrorHandler
{
    Task<ErrorHandlingResult> HandlePhaseErrorAsync(
        Exception error,
        OrchestrationContext context);
}

public class OrchestrationContext
{
    public string RequestId { get; set; }
    public int CurrentPhase { get; set; }
    public string? PreviousError { get; set; }
}

public class ErrorHandlingResult
{
    public bool ShouldContinue { get; set; }
    public string ErrorMessage { get; set; }
    public string? RecoveryAction { get; set; }
}
