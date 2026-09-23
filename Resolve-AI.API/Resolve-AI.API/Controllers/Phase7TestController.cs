using Microsoft.AspNetCore.Mvc;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;

namespace Resolve_AI.API.Controllers;

[ApiController]
[Route("api/phase7-test")]
public class Phase7TestController : ControllerBase
{
    private readonly IPhase7WorkflowService _phase7WorkflowService;
    private readonly IMicrosoft365IntegrationService _microsoft365IntegrationService;

    public Phase7TestController(
        IPhase7WorkflowService phase7WorkflowService,
        IMicrosoft365IntegrationService microsoft365IntegrationService)
    {
        _phase7WorkflowService = phase7WorkflowService;
        _microsoft365IntegrationService = microsoft365IntegrationService;
    }

    [HttpPost("uc07")]
    public async Task<IActionResult> TestUc07()
    {
        var request = Phase7WorkflowRequestMock.CreateUc07Request();

        var result = await _phase7WorkflowService.ProcessAsync(request);

        return Ok(result);
    }

    [HttpPost("uc08")]
    public async Task<IActionResult> TestUc08()
    {
        var request = Phase7WorkflowRequestMock.CreateUc08Request();

        var result = await _phase7WorkflowService.ProcessAsync(request);

        return Ok(result);
    }

    [HttpPost("invalid-requestid")]
    public async Task<IActionResult> TestInvalidRequestId()
    {
        var request = Phase7WorkflowRequestMock.CreateUc07Request();

        request.RequestId = string.Empty;

        var result = await _phase7WorkflowService.ProcessAsync(request);

        return Ok(result);
    }

    [HttpPost("invalid-user")]
    public async Task<IActionResult> TestInvalidUser()
    {
        var request = Phase7WorkflowRequestMock.CreateUc07Request();

        request.UserIdentifier = string.Empty;

        var result = await _phase7WorkflowService.ProcessAsync(request);

        return Ok(result);
    }

    [HttpPost("invalid-usecase")]
    public async Task<IActionResult> TestInvalidUseCase()
    {
        var request = Phase7WorkflowRequestMock.CreateUc07Request();

        request.UseCase = (resolve_AI.Domain.Enums.UseCaseType)999;

        var result = await _phase7WorkflowService.ProcessAsync(request);

        return Ok(result);
    }

    [HttpGet("microsoft365")]
    public async Task<IActionResult> TestMicrosoft365Integration()
    {
        var isAvailable =
            await _microsoft365IntegrationService.CheckAvailabilityAsync(
                "MOCK-TENANT");

        return Ok(new
        {
            IsAvailable = isAvailable,
            IsMock = true,
            Message = "Microsoft 365 mock integration is available."
        });
    }

    [HttpGet("microsoft365-user")]
    public async Task<IActionResult> TestMicrosoft365User()
    {
        var user =
            await _microsoft365IntegrationService.GetUserAsync(
                "test.user@mock.local",
                "MOCK-TENANT");

        return Ok(user);
    }
}