using Microsoft.AspNetCore.Mvc;
using resolve_AI.Application.Interfaces;

namespace Resolve_AI.API.Controllers;

[ApiController]
[Route("api/phase7-test")]
public class ExchangeTestController : ControllerBase
{
    private readonly IExchangeIntegrationService _exchangeService;

    public ExchangeTestController(
        IExchangeIntegrationService exchangeService)
    {
        _exchangeService = exchangeService;
    }

    [HttpGet("exchange")]
    public async Task<IActionResult> CheckExchange(
        CancellationToken cancellationToken)
    {
        try
        {
            var available = await _exchangeService.CheckAvailabilityAsync(
                cancellationToken);

            return Ok(new
            {
                IsAvailable = available,
                IsMock = false,
                Message = "Exchange connection verified."
            });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(504, new
            {
                IsAvailable = false,
                IsMock = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new
            {
                IsAvailable = false,
                IsMock = false,
                Message = ex.Message
            });
        }
    }
}
