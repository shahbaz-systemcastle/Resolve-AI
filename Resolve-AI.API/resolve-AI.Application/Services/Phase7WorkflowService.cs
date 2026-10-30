using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Validation;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Services;

/// <summary>
/// Workflow entry point for the License and Mailbox Management Module.
///
/// Step numbers follow the WBS list. 7.13a and 7.13b are extra steps that are
/// not in the WBS: alert setup and notification, both part of 7.13 validation.
///
/// UC-07 (license not assigned):
///   7.03 Request validation
///   7.04 Microsoft 365 availability
///   7.05 User lookup (user must exist and be enabled)
///   7.06 License status retrieval
///   7.07 License validation
///   7.08 Approved license assignment
///   7.09 Assignment verification (license is read back until visible)
///
/// UC-08 (mailbox capacity threshold alert):
///   7.03 Request validation
///   7.04 Microsoft 365 availability
///   7.05 User lookup
///   7.11 Mailbox usage retrieval (Graph usage report)
///   7.12 Mailbox capacity analysis
///   7.13 Quota evaluation (decides whether an alert is required)
///   7.13a Alert setup (creates an OPEN alert record; extra step)
///   7.13b Notification (through IMailboxAlertNotifier; default only logs; extra step)
///   7.14 Approved mailbox action (through IMailboxActionExecutor; default only logs,
///        provisional until Phase 3 and 4 define the action)
///   7.15 Mailbox action result reception (MailboxActionResultValidator; provisional)
///   7.16 Technical result preparation (the Phase7WorkflowResultDto returned here)
///
/// Not implemented yet: 7.17 (orchestration result handoff).
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

    private readonly IMailboxAlertNotifier _mailboxAlertNotifier;

    private readonly IMailboxActionExecutor _mailboxActionExecutor;

    private readonly IPhase7HandoffService _handoffService;

    private readonly IPhase8StorageService _storageService;

    public Phase7WorkflowService(
        IMicrosoft365IntegrationService microsoft365IntegrationService,
        IMailboxAlertNotifier mailboxAlertNotifier,
        IMailboxActionExecutor mailboxActionExecutor,
        IPhase7HandoffService handoffService,
        IPhase8StorageService storageService)
    {
        _microsoft365IntegrationService = microsoft365IntegrationService;
        _mailboxAlertNotifier = mailboxAlertNotifier;
        _mailboxActionExecutor = mailboxActionExecutor;
        _handoffService = handoffService;
        _storageService = storageService;
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
                var result = await ProcessMailboxCapacityAsync(
                    request,
                    user.UserIdentifier,
                    request.TenantIdentifier,
                    usesMock);
                return await FinalizeResultAsync(result);
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
                // 7.17 - Capture license snapshot even when no action is needed
                LicenseSnapshotDto? noActionSnapshot = null;
                try
                {
                    noActionSnapshot = await _microsoft365IntegrationService
                        .GetLicenseSubscriptionSnapshotAsync(request.TenantIdentifier);
                }
                catch
                {
                    // Snapshot is not critical, continue without it
                }

                var noActionResult = new Phase7WorkflowResultDto
                {
                    RequestId = request.RequestId,
                    UseCase = request.UseCase,
                    Success = true,
                    Status = Phase7Status.NoActionRequired,
                    Message = licenseValidation.Message,
                    IsMock = usesMock,
                    LicenseSnapshot = noActionSnapshot
                };
                return await FinalizeResultAsync(noActionResult);
            }

            // 7.08 - Approved License Action Execution

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

            // 7.09 - Assignment Result Reception: Graph accepting the request
            // is not enough, so the license is read back until it is visible.

            var verifiedStatus = await WaitForLicenseAsync(
                request.UserIdentifier,
                request.TenantIdentifier);

            // 7.17 - Capture license snapshot for audit trail
            LicenseSnapshotDto? licenseSnapshot = null;
            try
            {
                licenseSnapshot = await _microsoft365IntegrationService
                    .GetLicenseSubscriptionSnapshotAsync(request.TenantIdentifier);
            }
            catch
            {
                // Snapshot is not critical, continue without it
            }

            if (verifiedStatus == null)
            {
                // Microsoft 365 accepted the assignment, so the license may
                // exist. This is reported as UNVERIFIED, not as a plain failure.
                var unverifiedResult = new Phase7WorkflowResultDto
                {
                    RequestId = request.RequestId,
                    UseCase = request.UseCase,
                    Success = false,
                    Status = Phase7Status.AssignedUnverified,
                    Message = "Assignment was accepted by Microsoft 365, but the "
                              + $"license was not visible after {VerificationAttempts} "
                              + "checks. Check the license state before any retry.",
                    IsMock = usesMock,
                    LicenseSnapshot = licenseSnapshot
                };
                return await FinalizeResultAsync(unverifiedResult);
            }

            var completedResult = new Phase7WorkflowResultDto
            {
                RequestId = request.RequestId,
                UseCase = request.UseCase,
                Success = true,
                Status = Phase7Status.Completed,
                Message = $"User '{user.DisplayName}' resolved. "
                          + assignmentResult.Message
                          + " License verified: Business Premium with active Teams.",
                IsMock = usesMock,
                LicenseSnapshot = licenseSnapshot
            };
            return await FinalizeResultAsync(completedResult);
        }
        catch (Exception ex)
        {
            // A Microsoft 365 call failed. Report the real error instead
            // of treating it as "not found" or as a success.
            return Failure(request, Phase7Status.WorkflowError,
                $"Phase 7 workflow failed: {ex.Message}", usesMock);
        }
    }

    // UC-08: 7.11 mailbox usage, 7.12 capacity analysis.
    // The data comes from the Graph usage report, so no Exchange
    // connection is needed for this path.
    private async Task<Phase7WorkflowResultDto> ProcessMailboxCapacityAsync(
        Phase7WorkflowRequestDto request,
        string userPrincipalName,
        string? tenantIdentifier,
        bool usesMock)
    {
        // 7.11 - Mailbox Usage Retrieval

        var usage = await _microsoft365IntegrationService.GetMailboxUsageAsync(
            userPrincipalName,
            tenantIdentifier);

        usesMock = usesMock || usage.IsMock;

        if (!usage.Found)
        {
            return Failure(request, Phase7Status.MailboxDataNotFound,
                "No mailbox usage data was found for the user.", usesMock);
        }

        // 7.12 - Mailbox Capacity Analysis

        var capacity = MailboxCapacityAnalyzer.Analyze(usage);

        var analysed = capacity.State != "UNKNOWN";

        // 7.13 - Quota Evaluation (decision only; no alert is set up here)

        var quotaEvaluation = QuotaEvaluator.Evaluate(capacity);

        // 7.13a - Alert Setup (extra step; creates an OPEN record, nothing is sent)

        var alert = MailboxAlertSetup.Create(quotaEvaluation, DateTime.UtcNow);

        // 7.13b - Notification (extra step). Only runs when an alert exists.
        // The default notifier logs the alert and sends nothing.

        var notification = alert == null
            ? null
            : await _mailboxAlertNotifier.NotifyAsync(alert);

        // 7.14 - Approved Mailbox Action (provisional). Runs only when an alert
        // exists and the request is approved. Default executor only logs.

        MailboxActionResultDto? action = null;

        if (alert != null)
        {
            action = request.IsApproved == true
                ? await _mailboxActionExecutor.ExecuteAsync(alert)
                : new MailboxActionResultDto
                {
                    AlertId = alert.AlertId,
                    Status = Phase7Status.NotApproved,
                    Message = "Mailbox action is not approved; nothing was run."
                };

            // 7.15 - Mailbox Action Result Reception (checks the executor's answer)

            action = MailboxActionResultValidator.Receive(alert, action);
        }

        return new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = analysed,
            Status = analysed
                ? Phase7Status.CapacityAnalysed
                : Phase7Status.CapacityUnknown,
            Message = capacity.Message + " " + quotaEvaluation.Message
                      + (alert == null ? "" : $" Alert {alert.AlertId} is {alert.Status}; "
                          + $"notification {notification!.Status}; "
                          + $"mailbox action {action!.Status}."),
            QuotaEvaluation = quotaEvaluation,
            Alert = alert,
            AlertNotification = notification,
            MailboxAction = action,
            IsMock = usesMock
        };
    }

    // 7.09 - Re-reads the license until Business Premium with Teams is visible.
    // 7.17 - Orchestration Result Handoff
    // 8.12-8.18 - Phase 8 storage integration
    private async Task<Phase7WorkflowResultDto> FinalizeResultAsync(
        Phase7WorkflowResultDto result)
    {
        // Phase 8 WBS 8.12-8.18: Store request and result for audit trail
        try
        {
            // Save request (WBS 8.17)
            var dbRequestId = await _storageService.SaveRequestAsync(
                requestId: result.RequestId,
                userIdentifier: result.RequestId, // Will be updated from actual request
                useCase: (int)result.UseCase,
                tenantIdentifier: "default", // Will be from actual request
                isApproved: false); // Will be from actual request

            // Log initial action (WBS 8.15)
            await _storageService.LogActionAsync(
                requestDbId: dbRequestId,
                phase7RequestId: result.RequestId,
                action: "workflow_started",
                actionStatus: "success",
                details: $"Phase 7 workflow started for UseCase {result.UseCase}");

            // Save result (WBS 8.18)
            await _storageService.SaveResultAsync(
                requestDbId: dbRequestId,
                phase7RequestId: result.RequestId,
                status: result.Status,
                message: result.Message,
                isMock: result.IsMock,
                additionalData: SerializeAdditionalData(result));

            // Log completion (WBS 8.15)
            await _storageService.LogActionAsync(
                requestDbId: dbRequestId,
                phase7RequestId: result.RequestId,
                action: "workflow_completed",
                actionStatus: result.Success ? "success" : "failed",
                details: $"Phase 7 workflow completed with status: {result.Status}");
        }
        catch (Exception)
        {
            // Don't fail the workflow if storage fails; continue
        }

        return await _handoffService.PrepareHandoffAsync(result);
    }

    private static string SerializeAdditionalData(Phase7WorkflowResultDto result)
    {
        var data = new
        {
            quotaEvaluation = result.QuotaEvaluation,
            alert = result.Alert,
            alertNotification = result.AlertNotification,
            mailboxAction = result.MailboxAction,
            licenseSnapshot = result.LicenseSnapshot
        };

        return System.Text.Json.JsonSerializer.Serialize(data);
    }

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
