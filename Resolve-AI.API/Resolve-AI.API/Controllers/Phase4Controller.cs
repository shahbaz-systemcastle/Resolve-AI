namespace Resolve_AI.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Domain.Entities.Phase4;

[ApiController]
[Route("api/phase4")]
public class Phase4Controller : ControllerBase
{
    private readonly IPhase4OrchestrationService _orchestrationService;

    public Phase4Controller(IPhase4OrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    /// <summary>
    /// 4.01-4.12: Process automation request through Phase 4
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessRequest(
        [FromBody] AutomationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _orchestrationService.ProcessAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// 4.03: Determine routing for a request
    /// </summary>
    [HttpPost("determine-route")]
    public async Task<IActionResult> DetermineRoute([FromBody] AutomationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var route = await _orchestrationService.DetermineRouteAsync(request);

        return Ok(route);
    }
}
