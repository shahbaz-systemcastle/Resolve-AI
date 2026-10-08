using Microsoft.AspNetCore.Mvc;
using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Validation;

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

    // 7.2-7.7 Workflow Tests

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

    // 7.2-7.7 Complete Workflow (Generic)

    [HttpPost("process")]
    public async Task<IActionResult> ProcessWorkflow(
        [FromBody] Phase7WorkflowRequestDto request)
    {
        var result = await _phase7WorkflowService.ProcessAsync(request);
        return Ok(result);
    }

    // 7.2 - M365 Integration Check

    [HttpGet("microsoft365")]
    public async Task<IActionResult> TestMicrosoft365Integration()
    {
        var isAvailable =
            await _microsoft365IntegrationService
                .CheckAvailabilityAsync("MOCK-TENANT");

        return Ok(new
        {
            IsAvailable = isAvailable,
            IsMock = true,
            Message = "Microsoft 365 mock integration is available."
        });
    }

    // 7.3 - List All Users

    [HttpGet("microsoft365-users")]
    public async Task<IActionResult> TestMicrosoft365Users()
    {
        var users =
            await _microsoft365IntegrationService
                .GetUsersListAsync("MOCK-TENANT");

        return Ok(new
        {
            UserCount = users.Count,
            Users = users,
            Message = $"Retrieved {users.Count} users from Microsoft 365."
        });
    }

    // 7.4 - User Retrieval

    [HttpGet("microsoft365-user")]
    public async Task<IActionResult> TestMicrosoft365User()
    {
        var user =
            await _microsoft365IntegrationService.GetUserAsync(
                "test.user@mock.local",
                "MOCK-TENANT");

        return Ok(user);
    }

    // 7.5 - License Status Retrieval

    [HttpGet("microsoft365-license")]
    public async Task<IActionResult> TestMicrosoft365License()
    {
        var licenseStatus =
            await _microsoft365IntegrationService
                .GetLicenseStatusAsync(
                    "test.user@mock.local",
                    "MOCK-TENANT");

        return Ok(licenseStatus);
    }

    // 7.6 - License Status Validation

    [HttpGet("microsoft365-license-validation")]
    public async Task<IActionResult> TestMicrosoft365LicenseValidation()
    {
        var licenseStatus =
            await _microsoft365IntegrationService
                .GetLicenseStatusAsync(
                    "test.user@mock.local",
                    "MOCK-TENANT");

        var validationResult =
            Microsoft365LicenseStatusValidator.Validate(licenseStatus);

        return Ok(validationResult);
    }

    [HttpGet("microsoft365-license-validation-assigned")]
    public IActionResult TestMicrosoft365LicenseValidationAssigned()
    {
        // MOCK/TBD:
        // The current mock always returns HasAssignedLicense = false,
        // so this temporary status is used only to exercise the
        // "license already assigned" validation path.
        var licenseStatus = new Microsoft365LicenseStatusDto
        {
            UserIdentifier = "test.user@mock.local",
            HasAssignedLicense = true,
            LicenseStatus = "License assigned - MOCK/TBD",
            IsMock = true
        };

        var validationResult =
            Microsoft365LicenseStatusValidator.Validate(licenseStatus);

        return Ok(validationResult);
    }

    [HttpGet("microsoft365-license-validation-invalid")]
    public IActionResult TestMicrosoft365LicenseValidationInvalid()
    {
        var licenseStatus = new Microsoft365LicenseStatusDto
        {
            UserIdentifier = string.Empty,
            HasAssignedLicense = false,
            LicenseStatus = "No license assigned - MOCK/TBD",
            IsMock = true
        };

        var validationResult =
            Microsoft365LicenseStatusValidator.Validate(licenseStatus);

        return Ok(validationResult);
    }

    // 7.7 - License Assignment

    [HttpPost("microsoft365-license-assign")]
    public async Task<IActionResult> TestMicrosoft365LicenseAssign()
    {
        var assignmentResult =
            await _microsoft365IntegrationService.AssignLicenseAsync(
                "test.user@mock.local",
                "MOCK-TENANT",
                true);

        return Ok(assignmentResult);
    }

    [HttpPost("microsoft365-license-assign-not-approved")]
    public async Task<IActionResult> TestMicrosoft365LicenseAssignNotApproved()
    {
        var assignmentResult =
            await _microsoft365IntegrationService.AssignLicenseAsync(
                "test.user@mock.local",
                "MOCK-TENANT",
                false);

        return Ok(assignmentResult);
    }
}