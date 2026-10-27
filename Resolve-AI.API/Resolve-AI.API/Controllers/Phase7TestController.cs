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

    // Workflow tests with built-in requests (UC-07 and UC-08)

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

    // Complete workflow with a caller-supplied request

    [HttpPost("process")]
    public async Task<IActionResult> ProcessWorkflow(
        [FromBody] Phase7WorkflowRequestDto request)
    {
        var result = await _phase7WorkflowService.ProcessAsync(request);
        return Ok(result);
    }

    // 7.04 - Microsoft 365 availability check

    [HttpGet("microsoft365")]
    public async Task<IActionResult> TestMicrosoft365Integration()
    {
        var isAvailable =
            await _microsoft365IntegrationService
                .CheckAvailabilityAsync(null);

        return Ok(new
        {
            IsAvailable = isAvailable,
            Message = isAvailable
                ? "Microsoft 365 is available."
                : "Microsoft 365 is not available."
        });
    }

    // 7.05 - List all users (connection check)

    [HttpGet("microsoft365-users")]
    public async Task<IActionResult> TestMicrosoft365Users()
    {
        var users =
            await _microsoft365IntegrationService
                .GetUsersListAsync(null);

        return Ok(new
        {
            UserCount = users.Count,
            Users = users,
            Message = $"Retrieved {users.Count} users from Microsoft 365."
        });
    }
}
