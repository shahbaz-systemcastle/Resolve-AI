using resolve_AI.Application.DTOs;
using resolve_AI.Application.Interfaces;
using resolve_AI.Domain.Enums;

namespace resolve_AI.Application.Services;

public class Phase7WorkflowService : IPhase7WorkflowService
{
    public Task<Phase7WorkflowResultDto> ProcessAsync(
        Phase7WorkflowRequestDto request)
    {
        // 7.2 - Request Information Validation

        if (request is null)
        {
            return Task.FromResult(new Phase7WorkflowResultDto
            {
                Success = false,
                Message = "Phase 7 workflow request cannot be null.",
                IsMock = true
            });
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Task.FromResult(new Phase7WorkflowResultDto
            {
                RequestId = request.RequestId,
                UseCase = request.UseCase,
                Success = false,
                Message = "RequestId is required.",
                IsMock = true
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserIdentifier))
        {
            return Task.FromResult(new Phase7WorkflowResultDto
            {
                RequestId = request.RequestId,
                UseCase = request.UseCase,
                Success = false,
                Message = "UserIdentifier is required.",
                IsMock = true
            });
        }

        if (request.UseCase != UseCaseType.Microsoft365LicenseNotAssigned &&
            request.UseCase != UseCaseType.MailboxCapacityThresholdAlert)
        {
            return Task.FromResult(new Phase7WorkflowResultDto
            {
                RequestId = request.RequestId,
                UseCase = request.UseCase,
                Success = false,
                Message = "UseCase is not supported by Phase 7.",
                IsMock = true
            });
        }

        return Task.FromResult(new Phase7WorkflowResultDto
        {
            RequestId = request.RequestId,
            UseCase = request.UseCase,
            Success = true,
            Message = "Phase 7 workflow request validated successfully.",
            IsMock = true
        });
    }
}