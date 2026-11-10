namespace resolve_AI.Application.Services.Phase4;

using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;
using resolve_AI.Domain.Enums;

public class Phase4OrchestrationService : IPhase4OrchestrationService
{
    private readonly IPhase4RoutingResolver _routingResolver;
    private readonly IPhase5ExecutionProxy _phase5Proxy;
    private readonly IPhase6ExecutionProxy _phase6Proxy;
    private readonly IPhase7ExecutionProxy _phase7Proxy;
    private readonly IPhase4ResultCollector _resultCollector;
    private readonly IPhase4ErrorHandler _errorHandler;

    public Phase4OrchestrationService(
        IPhase4RoutingResolver routingResolver,
        IPhase5ExecutionProxy phase5Proxy,
        IPhase6ExecutionProxy phase6Proxy,
        IPhase7ExecutionProxy phase7Proxy,
        IPhase4ResultCollector resultCollector,
        IPhase4ErrorHandler errorHandler)
    {
        _routingResolver = routingResolver;
        _phase5Proxy = phase5Proxy;
        _phase6Proxy = phase6Proxy;
        _phase7Proxy = phase7Proxy;
        _resultCollector = resultCollector;
        _errorHandler = errorHandler;
    }

    public async Task<OrchestrationResult> ProcessAsync(
        AutomationRequest request,
        CancellationToken cancellationToken = default)
    {
        var orchestrationResult = new OrchestrationResult
        {
            RequestId = request.Id,
            Status = "Processing"
        };

        try
        {
            // 4.02: Validate request
            if (!ValidateRequest(request))
            {
                orchestrationResult.Status = "ValidationFailed";
                orchestrationResult.FinalError = "Invalid request format";
                return orchestrationResult;
            }

            // 4.03: Determine route
            var route = await DetermineRouteAsync(request);

            // 4.04-4.06: Execute appropriate phases
            var executionResults = await ExecutePhasesAsync(request, route, cancellationToken);
            orchestrationResult.PhaseResults = executionResults;

            // 4.07-4.10: Aggregate results
            var aggregatedResult = _resultCollector.AggregateResults();

            // Determine final status
            var hasErrors = executionResults.Any(r => r.Status == "Failed");
            orchestrationResult.Status = hasErrors ? "CompletedWithErrors" : "Success";

            return orchestrationResult;
        }
        catch (Exception ex)
        {
            // 4.11: Error handling
            var context = new OrchestrationContext
            {
                RequestId = request.Id,
                CurrentPhase = 4
            };

            var errorHandlingResult = await _errorHandler.HandlePhaseErrorAsync(ex, context);

            orchestrationResult.Status = "Failed";
            orchestrationResult.FinalError = errorHandlingResult.ErrorMessage;

            return orchestrationResult;
        }
    }

    public async Task<AutomationRoute> DetermineRouteAsync(AutomationRequest request)
    {
        return await _routingResolver.ResolveRouteAsync(request);
    }

    private bool ValidateRequest(AutomationRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId))
            return false;

        if (request.UseCase < UseCaseType.PasswordExpired)
            return false;

        return true;
    }

    private async Task<List<ExecutionResult>> ExecutePhasesAsync(
        AutomationRequest request,
        AutomationRoute route,
        CancellationToken cancellationToken)
    {
        var results = new List<ExecutionResult>();

        try
        {
            switch (route.TargetPhase)
            {
                case 5:
                    var phase5Result = await _phase5Proxy.ExecuteAsync(request, cancellationToken);
                    results.Add(phase5Result);
                    _resultCollector.AddResult(5, phase5Result);
                    break;

                case 6:
                    var phase6Result = await _phase6Proxy.ExecuteAsync(request, cancellationToken);
                    results.Add(phase6Result);
                    _resultCollector.AddResult(6, phase6Result);
                    break;

                case 7:
                    var phase7Result = await _phase7Proxy.ExecuteAsync(request, cancellationToken);
                    results.Add(phase7Result);
                    _resultCollector.AddResult(7, phase7Result);
                    break;

                default:
                    throw new ArgumentException($"Unknown target phase: {route.TargetPhase}");
            }
        }
        catch (Exception ex)
        {
            results.Add(new ExecutionResult
            {
                RequestId = request.Id,
                PhaseNumber = route.TargetPhase,
                Status = "Failed",
                Error = ex.Message,
                ExecutedAt = DateTime.UtcNow
            });
        }

        return results;
    }
}
