namespace resolve_AI.Application.Interfaces.Phase4;

using resolve_AI.Domain.Entities.Phase4;

public interface IPhase4RoutingResolver
{
    Task<AutomationRoute> ResolveRouteAsync(AutomationRequest request);
}
