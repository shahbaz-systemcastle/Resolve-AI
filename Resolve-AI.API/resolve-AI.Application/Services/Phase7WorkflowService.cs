using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Validation;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Services;

/// <summary>
/// Workflow entry point for the License and Mailbox Management Module.
///
/// This service orchestrates the Phase 7 steps in order:
///   7.2 Request Information Validation
///   7.3 Microsoft 365 Integration
///   7.4 User Information Retrieval
///   7.5 License Status Retrieval
///   7.6 License Status Validation
///   7.7 Approved License Action Execution
///
/// Not implemented yet:
///   7.8  License Action Result Reception
///   7.9  - 7.14 Exchange / Mailbox steps
///   7.15 - 7.16 Technical result and orchestration handoff
///
/// MOCK/TBD:
/// All Microsoft 365 calls currently go to the mock integration
/// service. The application flow is designed so that replacing the
/// mock with a real Microsoft Graph implementation requires no
/// change to this workflow.
/// </summary>
public class Phase7WorkflowService : IPhase7WorkflowService
{
    private readonly IMicrosoft365IntegrationService
        _microsoft365IntegrationService;

    public Phase7WorkflowService(
        IMicrosoft365IntegrationService microsoft365IntegrationService)
    {
        _microsoft365IntegrationService = microsoft365IntegrationService;
    }

    public async Task<Phase7WorkflowResultDto> ProcessAsync(
        Phase7WorkflowRequestDto request)
    {
        // 7.2 - Request Information Validation

        if (request is null)
        {
            return new Phase7WorkflowResultDto
            {
                Success = false,
                Message = "Phase 7 workflow request cannot be null.",
                IsMock = true
            };
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Failure(request, "RequestId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserIdentifier))
        {
            return Failure(request, "UserIdentifier is required.");
        }

        if (request.UseCase != UseCaseType.Microsoft365LicenseNotAssigned &&
            request.UseCase != UseCaseType.MailboxCapacityThresholdAlert)
        {
            return Failure(request, "UseCase is not supported by Phase 7.");
        }

        // 7.3 - Microsoft 365 Integration

        var isIntegrationAvailable =
            await _microsoft365IntegrationService.CheckAvailabilityAsync(
                request.TenantIdentifier);

        if (!isIntegrationAvailable)
        {
            return Failure(
                request,
                "Microsoft 365 integration is not available.");
        }

        // 7.4 - User Information Retrieval

        var user =
            await _microsoft365IntegrationService.GetUserAsync(
                request.UserIdentifier,
                request.TenantIdentifier);

        if (!user.Found)
        {
            return Failure(
                request,
                "Microsoft 365 user was not found for the supplied "
                + "UserIdentifier.");
        }

        // MOCK/TBD:
        // Mailbox handling (WBS 7.9 - 7.14) is not implemented yet,
        // so UC-08 stops here instead of returning a misleading success.
        if (request.UseCase == UseCaseType.MailboxCapacityThresholdAlert)
        {
            return Failure(
                request,
                "Mailbox capacity handling is not implemented yet "
                + "(WBS 7.9 - 7.14).");
        }

        // 7.5 - License Status Retrieval

        var licenseStatus =
            await _microsoft365IntegrationService.GetLicenseStatusAsync(
                request.UserIdentifier,
                request.TenantIdentifier);

        // 7.6 - License Status Validation

        var licenseValidation =
            Microsoft365LicenseStatusValidator.Validate(licenseStatus);

        if (!licenseValidation.IsValid)
        {
            return Failure(request, licenseValidation.Message);
        }

        // 7.7 - Approved License Action Execution

        var assignmentResult =
            await _microsoft365IntegrationService.AssignLicenseAsync(
                request.UserIdentifier,
                request.TenantIdentifier,
                request.IsApproved ?? false);

        if (!assignmentResult.Success)
        {
            return Failure(request, assignmentResult.Message);
        }

        // MOCK/TBD:
        // WBS 7.8 (License Action Result Reception) is not implemented
        // yet. The assignment result is received but not processed further.
        return new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = true,
            Message = $"User '{user.DisplayName}' resolved. "
                      + assignmentResult.Message
                      + " (WBS 7.8-7.16 pending).",
            IsMock = true
        };
    }

    private static Phase7WorkflowResultDto Failure(
        Phase7WorkflowRequestDto request,
        string message)
    {
        return new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = false,
            Message = message,
            IsMock = true
        };
    }
}