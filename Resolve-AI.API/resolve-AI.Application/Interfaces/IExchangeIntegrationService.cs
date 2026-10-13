namespace resolve_AI.Application.Interfaces;

/// <summary>
/// WBS 7.10 - Exchange Integration.
/// </summary>
public interface IExchangeIntegrationService
{
    Task<bool> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default);
}
