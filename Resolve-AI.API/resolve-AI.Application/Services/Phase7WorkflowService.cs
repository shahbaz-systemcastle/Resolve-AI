using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Validation;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Services;

/// <summary>
/// Workflow entry point for the License and Mailbox Management Module.
///
/// UC-07 (license not assigned):
///   7.03 Request validation
///   7.04 Microsoft 365 availability
///   7.05 User lookup (user must exist and be enabled)
///   7.06 License status retrieval
///   7.07 License validation and approved assignment
///   7.08 Assignment verification (license is read back until visible)
///
/// UC-08 (mailbox capacity threshold alert):
///   7.03 Request validation
///   7.04 Microsoft 365 availability
///   7.05 User lookup
///   7.09 Mailbox usage retrieval (Graph usage report)
///   7.10 Mailbox capacity analysis
///   7.11 Quota evaluation (decides whether an alert is required)
///
/// Not implemented yet: 7.12 - 7.14 (alert setup, notification,
/// monitoring) and 7.15 - 7.16 (orchestration handoff).
///
/// Microsoft 365 calls go through IMicrosoft365IntegrationService.
/// IsMock in the result reports whether the mock answered.
/// </summary>
public class Phase7WorkflowService : IPhase7WorkflowService
{
    // MOCK/TBD: Graph can take a while to show a new license.
    private const int VerificationAttempts = 5;

    private static readonly TimeSpan VerificationDelay = TimeSpan.FromSeconds(5);

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
        // 7.03 - Request Information Validation

        if (request is null)
        {
            return new Phase7WorkflowResultDto
            {
                Success = false,
                Status = Phase7Status.ValidationFailed,
                Message = "Phase 7 workflow request cannot be null.",
                IsMock = false
            };
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Failure(request, Phase7Status.ValidationFailed,
                "RequestId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserIdentifier))
        {
            return Failure(request, Phase7Status.ValidationFailed,
                "UserIdentifier is required.");
        }

        if (request.UseCase != UseCaseType.Microsoft365LicenseNotAssigned &&
            request.UseCase != UseCaseType.MailboxCapacityThresholdAlert)
        {
            return Failure(request, Phase7Status.ValidationFailed,
                "UseCase is not supported by Phase 7.");
        }

        // Tracks whether any Microsoft 365 answer came from the mock.
        var usesMock = false;

        // 7.04 - Microsoft 365 availability.
        // A Graph error gets its own status instead of a generic failure.

        try
        {
            var isIntegrationAvailable =
                await _microsoft365IntegrationService.CheckAvailabilityAsync(
                    request.TenantIdentifier);

            if (!isIntegrationAvailable)
            {
                return Failure(request, Phase7Status.Microsoft365Unavailable,
                    "Microsoft 365 integration is not available.");
            }
        }
        catch (Exception ex)
        {
            return Failure(request, Phase7Status.Microsoft365Unavailable,
                $"Microsoft 365 is not reachable: {ex.Message}");
        }

        try
        {
            // 7.05 - User Information Retrieval

            var user =
                await _microsoft365IntegrationService.GetUserAsync(
                    request.UserIdentifier,
                    request.TenantIdentifier);

            usesMock = user.IsMock;

            if (!user.Found)
            {
                return Failure(request, Phase7Status.UserNotFound,
                    "Microsoft 365 user was not found for the supplied "
                    + "UserIdentifier.", usesMock);
            }

            // 7.05 - A disabled account must not receive or keep licenses.
            if (!user.AccountEnabled)
            {
                return Failure(request, Phase7Status.UserDisabled,
                    $"User '{user.DisplayName}' is disabled in Microsoft 365.",
                    usesMock);
            }

            if (request.UseCase == UseCaseType.MailboxCapacityThresholdAlert)
            {
                return await ProcessMailboxCapacityAsync(
                    request,
                    user.UserIdentifier,
                    request.TenantIdentifier,
                    usesMock);
            }

            // 7.06 - License Status Retrieval

            var licenseStatus =
                await _microsoft365IntegrationService.GetLicenseStatusAsync(
                    request.UserIdentifier,
                    request.TenantIdentifier);

            usesMock = usesMock || licenseStatus.IsMock;

            // 7.07 - License Status Validation

            var licenseValidation =
                Microsoft365LicenseStatusValidator.Validate(licenseStatus);

            if (!licenseValidation.IsValid)
            {
                return Failure(request, Phase7Status.LicenseReviewRequired,
                    licenseValidation.Message, usesMock);
            }

            // The user already has a license, so no action is taken.
            if (!licenseValidation.LicenseActionRequired)
            {
                return new Phase7WorkflowResultDto
                {
                    RequestId = request.RequestId,
                    UseCase = request.UseCase,
                    Success = true,
                    Status = Phase7Status.NoActionRequired,
                    Message = licenseValidation.Message,
                    IsMock = usesMock
                };
            }

            // 7.07 - Approved License Action Execution

            var isApproved = request.IsApproved ?? false;

            if (!isApproved)
            {
                return Failure(request, Phase7Status.NotApproved,
                    "Business Premium assignment is not approved.", usesMock);
            }

            // A license cannot be assigned until the user has a usage location.
            if (string.IsNullOrWhiteSpace(user.UsageLocation))
            {
                return Failure(request, Phase7Status.UsageLocationMissing,
                    "User has no usage location set in Microsoft 365, "
                    + "which is required before a license can be assigned.",
                    usesMock);
            }

            var assignmentResult =
                await _microsoft365IntegrationService.AssignLicenseAsync(
                    request.UserIdentifier,
                    request.TenantIdentifier,
                    isApproved);

            usesMock = usesMock || assignmentResult.IsMock;

            if (!assignmentResult.Success)
            {
                // The assignment status is a specific code such as
                // NO_FREE_SEATS or SKU_NOT_ENABLED; pass it through.
                return Failure(request, assignmentResult.AssignmentStatus,
                    assignmentResult.Message, usesMock);
            }

            // 7.08 - Assignment Result Reception: Graph accepting the request
            // is not enough, so the license is read back until it is visible.

            var verifiedStatus = await WaitForLicenseAsync(
                request.UserIdentifier,
                request.TenantIdentifier);

            if (verifiedStatus == null)
            {
                // Microsoft 365 accepted the assignment, so the license may
                // exist. This is reported as UNVERIFIED, not as a plain failure.
                return new Phase7WorkflowResultDto
                {
                    RequestId = request.RequestId,
                    UseCase = request.UseCase,
                    Success = false,
                    Status = Phase7Status.AssignedUnverified,
                    Message = "Assignment was accepted by Microsoft 365, but the "
                              + $"license was not visible after {VerificationAttempts} "
                              + "checks. Check the license state before any retry.",
                    IsMock = usesMock
                };
            }

            return new Phase7WorkflowResultDto
            {
                RequestId = request.RequestId,
                UseCase = request.UseCase,
                Success = true,
                Status = Phase7Status.Completed,
                Message = $"User '{user.DisplayName}' resolved. "
                          + assignmentResult.Message
                          + " License verified: Business Premium with active Teams. "
                          + "(WBS 7.12-7.16 pending.)",
                IsMock = usesMock
            };
        }
        catch (Exception ex)
        {
            // A Microsoft 365 call failed. Report the real error instead
            // of treating it as "not found" or as a success.
            return Failure(request, Phase7Status.WorkflowError,
                $"Phase 7 workflow failed: {ex.Message}", usesMock);
        }
    }

    // UC-08: 7.09 mailbox usage, 7.10 capacity analysis.
    // The data comes from the Graph usage report, so no Exchange
    // connection is needed for this path.
    private async Task<Phase7WorkflowResultDto> ProcessMailboxCapacityAsync(
        Phase7WorkflowRequestDto request,
        string userPrincipalName,
        string? tenantIdentifier,
        bool usesMock)
    {
        // 7.09 - Mailbox Usage Retrieval

        var usage = await _microsoft365IntegrationService.GetMailboxUsageAsync(
            userPrincipalName,
            tenantIdentifier);

        usesMock = usesMock || usage.IsMock;

        if (!usage.Found)
        {
            return Failure(request, Phase7Status.MailboxDataNotFound,
                "No mailbox usage data was found for the user.", usesMock);
        }

        // 7.10 - Mailbox Capacity Analysis

        var capacity = MailboxCapacityAnalyzer.Analyze(usage);

        var analysed = capacity.State != "UNKNOWN";

        // 7.11 - Quota Evaluation (decision only; no alert is set up here)

        var quotaEvaluation = QuotaEvaluator.Evaluate(capacity);

        return new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = analysed,
            Status = analysed
                ? Phase7Status.CapacityAnalysed
                : Phase7Status.CapacityUnknown,
            Message = capacity.Message + " " + quotaEvaluation.Message
                      + " (WBS 7.12-7.16 pending.)",
            QuotaEvaluation = quotaEvaluation,
            IsMock = usesMock
        };
    }

    // 7.08 - Re-reads the license until Business Premium with Teams is visible.
    private async Task<Microsoft365LicenseStatusDto?> WaitForLicenseAsync(
        string userIdentifier,
        string? tenantIdentifier)
    {
        for (var attempt = 1; attempt <= VerificationAttempts; attempt++)
        {
            var status =
                await _microsoft365IntegrationService.GetLicenseStatusAsync(
                    userIdentifier,
                    tenantIdentifier);

            if (status.HasRequiredLicense && status.IsTeamsEnabled == true)
            {
                return status;
            }

            if (attempt < VerificationAttempts)
            {
                await Task.Delay(VerificationDelay);
            }
        }

        return null;
    }

    private static Phase7WorkflowResultDto Failure(
        Phase7WorkflowRequestDto request,
        string status,
        string message,
        bool isMock = false)
    {
        return new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = false,
            Status = status,
            Message = message,
            IsMock = isMock
        };
    }
}
