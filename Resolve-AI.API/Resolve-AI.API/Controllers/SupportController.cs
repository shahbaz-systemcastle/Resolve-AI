using Microsoft.AspNetCore.Mvc;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace resolve_AI.API.Controllers;

[ApiController]
[Route("api/requests")]
public class SupportController : ControllerBase
{
    private readonly ISupportRequestService _supportRequestService;

    public SupportController(
        ISupportRequestService supportRequestService)
    {
        _supportRequestService = supportRequestService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessRequest(
        [FromBody] SupportRequestDto request)
    {
        var response = await _supportRequestService
            .ProcessRequestAsync(request);

        return Ok(response);
    }
}